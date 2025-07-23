using gdio.common.objects;
using gdio.protocol.objects.AppLayer.CmdRequests;
using gdio.unreal_api;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SmokeTest

{
    public partial class Tests
    {
        [DllImport("user32.dll")]
        public static extern int SetForegroundWindow(int hwnd);
        public static bool standalone = false;

        //These parameters can be used to override settings used to test when running from the NUnit command line
        public string testMode = TestContext.Parameters.Get("Mode", "IDE");
        public string editorVersion = TestContext.Parameters.Get("EditorVersion", "5.3"); 
        public string testProjName = TestContext.Parameters.Get("ProjectName", "LyraStarterGame"); 
        public string pathToExe = TestContext.Parameters.Get("PathToExe", null);


        ApiClient api;


        [OneTimeSetUp]
        public void Setup()
        {
            if (standalone)
            {
                Process[] appsrunning = Process.GetProcessesByName(testProjName);
                if (appsrunning.Length == 0)
                {
                    Process unreal = System.Diagnostics.Process.Start(pathToExe);
                }
                else
                {
                    SetForegroundWindow((int)appsrunning[0].MainWindowHandle);
                }
            }
            else
            {
                Process[] appsrunning;
                if (editorVersion == "4.27")
                    appsrunning = Process.GetProcessesByName("UE4Editor");
                else
                    appsrunning = Process.GetProcessesByName("UnrealEditor");

                if (appsrunning.Length == 0)
                {
                    Console.WriteLine($"If you want to test in the editor, launch the editor and start the game first");
                    Assert.Fail();//process has to run, and the PIE editor running. 
                                  
                }
                else
                {
                    SetForegroundWindow((int)appsrunning[0].MainWindowHandle);
                }
            }

            api = new ApiClient();
            api.Connect("localhost", 15505 ,true ,30);
      

            
        }


        [Test, Order(0)]
        public void MainMenuTest()
        {
            Console.WriteLine("Start MainMenuTest");


            api.LoadLevel("L_LyraFrontEnd",30);
            api.Wait(1000);
         

            Assert.IsTrue(api.WaitForObject("//*[@name = 'W_LyraFrontEnd_C_0']" , 30)  , "LyraFrontEnd Widget is not loaded"); // Check for LyraFrontEnd Widget
            
            Assert.IsTrue(api.WaitForObject("//QuitGameButton", 30) , "Quit button is not available"); // Validate Quit Button exist
            Assert.IsTrue(api.WaitForObject("//OptionsButton", 30)  , "Option button is not available"); // Validate Options Button exist
            Assert.IsTrue(api.WaitForObject("//StartGameButton", 30)  , "Start button is not available"); // Validate PlayButton exist

            api.Wait(1000);
            api.ClickObject(MouseButtons.LEFT , "//StartGameButton", 30);

            api.Wait(1000);
            Assert.IsTrue(api.WaitForObject("//QuickplayButton", 30), "Quick play button is not available"); // Validate QuickPlay exist
            api.ClickObject(MouseButtons.LEFT , "//QuickplayButton", 30);
            api.Wait(1000);
            
            

        }


        [Test,Order(1)]
        public void ShootWeaponTest()
        {
            //GetCurrentAmmoCount is a helper function designe to get the ammo from the widget
            int GetCurrentAmmoCount()
            {
                
                string numAmmo = api.GetObjectFieldValue<string>("//AmmoLeftInMagazineWidget/@text", 30);

                return  Int32.Parse(numAmmo);
            }


            // Start of the logic for the shooting test

            Console.WriteLine("Start ShootWeaponTest");

            api.CreateInputDevice("GDIO", "IMC_Default,IMC_ShooterGame");
            api.LoadLevel("L_ShooterGym");
            api.Wait(1000);

            int StartAmmo = GetCurrentAmmoCount();
            
            api.KeyPress(new KeyCode[] { KeyCode.LeftMouseButton });
            api.Wait(1000);

            Assert.IsTrue(StartAmmo > GetCurrentAmmoCount(), "The charcter didn't shoot");

        }


        [Test, Order(2)]
        public void PointToLocationTest()
        {
            // MoveMouse is a helper function designed to be used instead of Vector2InputEvent
            bool MoveMouse(string x , string y , Vector2 direction , ulong numberOfFrames , int timeout)
            {
                bool InputX;
                bool InputY;

                //We are using FloatInputEvent for the both of directions of a Mouse
                InputX = api.FloatInputEvent(x, direction.x ,numberOfFrames , timeout);
                InputY = api.FloatInputEvent(y, direction.y ,numberOfFrames , timeout);

                if (InputX == true && InputY == true)
                    return true;
                else
                    return false;
            }
            // Normalize is a helper function designed to normalize a vector3
            Vector3 Normalize(Vector3 vector)
            {
                float lenght = (float)Math.Sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z);
                return new Vector3(vector.x / lenght, vector.y / lenght, vector.z );
            }
            //End of helper functions



            //Start of Point to Enemy test

            bool PointToEnemy = false;

          
            api.CreateInputDevice("GDIO", "IMC_Default,IMC_ShooterGame");
            api.LoadLevel("L_ShooterGym");
            api.Wait(1000);

          
             while (PointToEnemy == false)
            {   
                // Get player's current rotation
                
                Vector3 playerRot = api.GetObjectRotation("//*[@name = 'B_Hero_ShooterMannequin_C_0']");
                float currentYaw = playerRot.z;
                float currentPitch = playerRot.x;

                //Get direction to target
                Vector3 playerPos = api.GetObjectPosition("//*[@name = 'B_Hero_ShooterMannequin_C_0']", CoordinateConversion.Local);
                Vector3 targetPos = api.GetObjectPosition("//*[@name = 'B_Hero_ShooterMannequin_C_1']", CoordinateConversion.Local);
                Vector3 direction = Normalize(targetPos - playerPos);

                float desiredYaw = MathF.Atan2(direction.y, direction.x) * (180 / MathF.PI);
                float desiredPitch = MathF.Asin(direction.z) * (180 / MathF.PI);

                float yawError = MathF.Abs(desiredYaw - currentYaw);
                float pitchError = MathF.Abs(desiredPitch - currentPitch);
            
                if (yawError < 2.0f && pitchError < 2.0f) //2.0f is the tollerance
                {
                    MoveMouse("MouseX", "MouseY", Vector2.zero, 30, 30);
                    PointToEnemy = true;

                }

                MoveMouse("MouseX", "MouseY", new Vector2((desiredYaw - currentYaw) * 0.7f, (desiredPitch - currentPitch) * 0.7f), 30, 30); // 0.7f speed

            }   
            
            Assert.IsTrue( PointToEnemy , "The player didn't point towards the enemy" ); 
        }


        [Test , Order(3)]
        public void MovementTest()
        {
            Console.WriteLine("Start MovementTest");

            api.CreateInputDevice("GDIO", "IMC_Default,IMC_ShooterGame");
            api.LoadLevel("L_ShooterGym");
            api.Wait(500);

            Vector3 initialSpot = api.GetObjectPosition("//*[@name = 'B_Hero_ShooterMannequin_C_0']" , CoordinateConversion.Local); 
            Console.WriteLine(initialSpot);

            api.KeyPress(new KeyCode[] { KeyCode.W } , 0);

            api.Wait(500);

            Vector3 currentSpot = api.GetObjectPosition("//*[@name = 'B_Hero_ShooterMannequin_C_0']", CoordinateConversion.Local);
            Console.WriteLine(currentSpot);

            Assert.IsTrue(currentSpot != initialSpot, "Caracter didn't move");
        
        }
        

        [Test, Order(4)]
        public void PlayerCanBeKilledTest()
        {
            Console.WriteLine("Start PlayerCanBeKilledTest");

            int numAttempts = 0; // number of atempts
            int maxAttempts = 25; // maximum number of atempts

            api.CreateInputDevice("GDIO", "IMC_Default,IMC_ShooterGame");
            api.LoadLevel("L_ShooterGym");
            api.Wait(1000);
            Assert.IsTrue(api.WaitForObject("//*[@name = 'B_Hero_ShooterMannequin_C_0']" ,10) , "Charcter is not spawned");


            //use a for loop for the atempts with 1 second wait beteween atempts max 25 atempts
           while(api.WaitForObject("//*[@name = 'B_Hero_ShooterMannequin_C_0']" , 1) == true && numAttempts != maxAttempts )
            {
                numAttempts++;
                api.Wait(1000);
            }


            Assert.IsFalse(api.WaitForObject("//*[@name = 'B_Hero_ShooterMannequin_C_0']" ,1) , "Character was not killed");

        }

        [OneTimeTearDown]
        public void Disconnect()
        {
            if (!standalone)
            {
                Console.WriteLine("Stoping The editor");
                api.StopEditorPlay();
            }

            Console.WriteLine("Disconecting ");
            api.Disconnect();
        }
    }
}
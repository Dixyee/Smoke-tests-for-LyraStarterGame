# Smoke test for LyraStarterGame

## This is Smoke test for LyraStartGame it contains 5 tests that need to be executed:
- [**MainMenu Test**](#mainmenu-test)
- [**Shoot Test**](#shoot-test)
- [**PointToLocation Test**](#pointtolocation-test)
- [**Movement Test**](#movement-test)
- [**PlayerCanBeKilled Test**](#playercanbekilled-test)

### MainMenu Test

This test validate that the following buttons exist:
- Quit button
- Options button
- Play button
- Quick play button 


```csharp
Assert.IsTrue(api.WaitForObject("//QuitGameButton", 30) , "Quit button is not available"); // Validate Quit Button exist

Assert.IsTrue(api.WaitForObject("//OptionsButton", 30)  , "Option button is not available"); // Validate Options Button exist

Assert.IsTrue(api.WaitForObject("//StartGameButton", 30)  , "Start button is not available"); // Validate Play button exist
```
After the validation we click on the **play button** 

```csharp
api.ClickObject(MouseButtons.LEFT , "//StartGameButton", 30);
```

We validate that the **quick play button** appears and we click on it

```csharp
api.ClickObject(MouseButtons.LEFT , "//StartGameButton", 30);
```

### Shoot Test

This test validates that the player can use the weapon.

We need to create a function that gets the amount of ammo 

```csharp
int GetCurrentAmmoCount()
{
    string numAmmo = api.GetObjectFieldValue<string>("//AmmoLeftInMagazineWidget/@text", 30);

    return  Int32.Parse(numAmmo);
}
```

We need to store the amount when we spawn and compare it after we shoot the weapon
```csharp
int StartAmmo = GetCurrentAmmoCount();

api.KeyPress(new KeyCode[] { KeyCode.LeftMouseButton });
api.Wait(1000);

Assert.IsTrue(StartAmmo > GetCurrentAmmoCount(), "The charcter didn't shoot");
```

### PointToLocation Test
Now we are gone validate that the player can move the camera towards the enemy.

First we need to create a function the will react as Vector2InputEvent beceause this function is not working, and will create another function to normalize a vector3

```csharp
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
Vector3 Normalize(Vector3 vector)
{
    float lenght = (float)Math.Sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z);
    return new Vector3(vector.x / lenght, vector.y / lenght, vector.z );
}
```

I am using a while loop for this test so we can get every time the location and rotation of the player(**B_Hero_ShooterMannequin_C_0**) and the location of the target(**B_Hero_ShooterMannequin_C_1**).

Knowing this will help aim for our target using **MoveMouse**

```csharp
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
```

Now we only need to check if the **PointToEnemy** is true

```csharp
Assert.IsTrue( PointToEnemy , "The player didn't point towards the enemy" ); 
```

### Movement Test
This test validates that the player can control the character using the keyboard.

First we are geting the intial spot of the charter(**B_Hero_ShooterMannequin_C_0**)
```csharp
Vector3 initialSpot = api.GetObjectPosition("//*[@name = 'B_Hero_ShooterMannequin_C_0']" , CoordinateConversion.Local); 
```
After we get the position we are gone move the charter and get the new position
```csharp
api.KeyPress(new KeyCode[] { KeyCode.W } , 0);
api.Wait(500);
Vector3 currentSpot = api.GetObjectPosition("//*[@name = 'B_Hero_ShooterMannequin_C_0']", CoordinateConversion.Local);Assert.IsTrue(currentSpot != initialSpot, "Caracter didn't move");
```
Now we just need to validadete that the player moved by comparing the intial position(**initialSpot**) and current position(**currentSpot**)
```csharp
Assert.IsTrue(currentSpot != initialSpot, "Caracter didn't move");
```

### PlayerCanBeKilled Test
The last test that we are gone do is to check if the player can be killed.

For this test we are using number of attempts(**numAtempts**) and the max number of attempts(**maxAtempts**), we are gone use them inside of a while loop so we can reduce the time of the test. If we were to use **Wait()** function we will need to wait for that specific amount of time every time we run the test even though the player can die before the time is up.If the number of attempts is equal to the maximum number the test will fail.

After we loading in to the map we check if the player(**B_Hero_ShooterMannequin_C_0**) is spawned

```csharp
Assert.IsTrue(api.WaitForObject("//*[@name = 'B_Hero_ShooterMannequin_C_0']" ,10) , "Charcter is not spawned");
```

Now we are gone check every second if the player is dead or the maximum number of attempts is reached

```csharp
 //use a for loop for the atempts with 1 second wait beteween atempts max 25 atempts
while(api.WaitForObject("//*[@name = 'B_Hero_ShooterMannequin_C_0']" , 1) == true && numAttempts != maxAttempts )
 {
     numAttempts++;
     api.Wait(1000);
 }
```
When the while loop is finished we need to validate if the player died
```csharp
Assert.IsFalse(api.WaitForObject("//*[@name = 'B_Hero_ShooterMannequin_C_0']" ,1) , "Character was not killed");
```
I am checking for **B_Hero_ShooterMannequin_C_0**, because every time a charter dies the instance of the AActor **B_Hero_ShooterMannequin_C_0** is destroyed.

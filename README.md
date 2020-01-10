# Processor-Profile System
### Architecture Pattern for Unity

![banner](https://media.discordapp.net/attachments/607007438180450305/662457107634847777/unknown.png)

## How to install
You can install PPS through the Unity Package Manager (UPM):

1. Window -> Package Manager
2. \+ (Add package button) -> Add Package from git URL
3. Use the URL `https://github.com/rellfy/PPS.git`

## Architecture

The Processor-Profile System, or PPS, is an architectural software pattern designed to be implemented in Unity projects. The main goal is to reduce MonoBehaviour script usage in favour of a conceptually simpler, faster and more structured codebase which will favour development efficiency and project management.

As you might have guessed, this pattern consists of three main components, of which consist of the following:

### System
A [System](https://github.com/rellfy/PPS/blob/master/Runtime/System.cs) is, essentially, a factory. It deploys instances which are a pair of `Processor` and `Profile`. The `System` also carries system-wide constants which are accessible by its intances. It inherits from Monobehaviour, and passes MonoBehaviour calls such as `Update` and `FixedUpdate` to its intances' processors.

#### Subsystem
There are also subsystems. These are simillar to Systems, but they do not inherit from MonoBehaviour. A `Subsystem` can only be initialised by a `System`, and it only has references to its instances and the `System` that initialised it. It can also carry system-wide constants as ScriptableObjects, which is useful for switching a System's configuration.

### Profile
A [Profile](https://github.com/rellfy/PPS/blob/master/Runtime/Profile.cs) is a a simple serializable class which holds a reference to the instance's GameObject and the entire state of that instance. There can also be sub-profiles which help in keeping the project modular. It can fire events related to the state held. You can opt to add an aditional `TSystem system` parameter to the Profile's constructor if it was deployed via a System (i.e. `Profile profile = ((Processor)System.DeployInstance()).Profile` - this is handled by the System factory, you do not need to pass any extra parameters when deploying the system.

### Processor
A [Processor](https://github.com/rellfy/PPS/blob/master/Runtime/Processor.cs) handles the instance's logic by managing the instance's state through the `Profile`. It may fire events related to the processed logic. A Processor can have Subprocessors which are of the same type, and automatically managed by the Processor instance as long as the base `Update/FixedUpdate/etc` methods are called if overriding - example of that [here](https://github.com/rellfy/PPSDemo/blob/master/Assets/PPS%20Demo/Systems/UI/Processors/UIProcessor.cs).

## Implementation
This pattern was designed with the idea of Systems that operate either dependently or independently. A dependent system needs a reference to another system(s). This way, a complete game can operate based on just three, four or five systems depending on complexity:
  
**1. World**  
**2. UI**  
**3. Audio**  
**4. Network**  
**5. Achievement**  

Check out the [example project](https://github.com/rellfy/PPSDemo.git) which features a simple but fully working game with UI, Audio and World Systems.

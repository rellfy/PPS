# PPS
Processor-Profile System: software architecture pattern for Unity

## Architecture

The Processor-Profile System, or PPS, is an architectural software pattern designed to be implemented in Unity projects. The main goal is to reduce MonoBehaviour script usage in favour of a conceptually simpler, faster and more structured codebase which will favour development efficiency and project management.

As you might have guessed, this pattern consists of three main components, of which consist of the following:

### System
A `System` is, essentially, a factory. It deploys instances which are a pair of `Processor` and `Profile`. The `System` also carries system-wide constants which are accessible by its intances. It inherits from Monobehaviour, and passes MonoBehaviour calls such as `Update` and `FixedUpdate` to its intances' processors.

#### Subsystem
There are also subsystems. These are simillar to Systems, but they do not inherit from MonoBehaviour. A `Subsystem` can only be initialised by a `System`, and it only has references to its instances and the `System` that initialised it. It can also carry system-wide constants.

### Profile
A profile is a a simple serializable class which holds a reference to the instance's GameObject and the entire state of that instance. There can also be sub-profiles which help in keeping the project modular.

### Processor
A processor handles the instance's logic by managing the instance's state through the `Profile`. 

## Implementation

An example project is to be included which will demonstrate an usage of this architectural pattern.

# Core Concepts and Assets (Oculus Version)

## AR Scene
Scene that will be displayed to the user as AR content from the HMDSimulator

## Real Scene
Scene that the users will view the AR content from as a virtually "real-world" environment.

## Main Scene
Scene that will be run, which will combine the "AR" and the "Real" Scene and run them as one scene with content from the AR Scene viewable from the HMD.

## Tracked Object
Object in AR Scene that will be tracked and overlayed on an object in the "Real" environment with a Tracker Behaviour with the corresponding tracker label.

## Tracker Behavior
A behaviour that can be applied to an object in the Real scene to have Tracked objects with the corresponding tracker labels to become anchored to it when viewed through the HMDSimulator.

## Main_HMDSimulatorManager
The single prefab needed to establish a Main scene.
It contains the MainManager script and the TrackerManager script.
### MainManager
- Creates a scene that contains the Real Scene and AR Scene, and displays parts of the AR Scene on the HMDSimulator in the Real Scene section based on Trackers and Tracked Objects.
### TrackerManager
- Manages the trackers(Real) and tracked objects(AR) in the two scenes and connects the two such that in the HMD Simulator, the Tracked Objects appear anchored to the objects with Tracker behavior.
The only user input required in this prefab is the Scene prefix of the two scenes (AR and Real Scene)

## OVRSetAR
The single prefab needed to establish an AR scene.
It contains Tracker objects for the HMD and Controllers which allows
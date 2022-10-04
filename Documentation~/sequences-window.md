# Sequences window

Use the Sequences window to create, organize and browse through the [Editorial structure](concepts.md#editorial) of the Sequences you’re working on.

To open the Sequences window, from the main menu of the Editor, select **Window > Sequencing > Sequences**.

![](images/sequences-window.png)

The Sequences window mainly allows you to:

* [Set up the overall structure](#creating-a-sequence) of your cinematics.

* [Browse through all Sequences](#browsing-through-sequences) that compose your cinematics.

Additionally, from this window:

* You can manage the [activation of additional Scenes](#setting-up-scene-activation-in-a-sequence) in the context of specific Sequences.

* You can access the Unity Recorder to [record any Sequence](#recording-a-sequence) and export it, for example, to a video file.

### Creating a Sequence

To create a Master Sequence, select the **+** (plus) button at the top left of the Sequences window, then select **Create Master Sequence**.
>**Note:** Regardless of whether a Sequence is selected in the window, the new Master Sequence will appear in alphabetical order at the same level as other Master Sequences in the Sequences window.

To create a Sequence within another Sequence:
1. Select the Sequence (or Master Sequence) to contain the one you want to create.
2. Select the **+** (plus) button at the top left of the Sequences window and select **Create Sequence**.

  >**Note:** If the parent Sequence is a Prefab, you might need to apply overrides from the Prefab instance to the Prefab asset to keep them synchronized. See [how to manage Sequences as Prefabs](sequences-as-prefabs.md).

When the new Sequence appears in the structure, you can directly rename it and press the Enter key or click anywhere to complete the creation.

Once you have created a new Sequence, you can use the [Sequence Assembly window](sequence-assembly-window.md) to populate it with Sequence Assets or Variants from your [Asset Collections](sequence-assets-window.md).

### Renaming a Sequence

To rename a Sequence:
1. Right-click the sequence and select **Rename**.
2. Type the new name.
3. Press the Enter key or click anywhere to confirm the renaming.

>**Note:**
>* The Sequences window currently prevents you from renaming a Sequence that is a Prefab (see the [suggested workaround](known-issues.md)).
>* If you rename a Sequence within a Sequence that is a Prefab, you might need to apply overrides from the Prefab instance to the Prefab asset to keep them synchronized. See [how to manage Sequences as Prefabs](sequences-as-prefabs.md).

### Deleting Sequences

To delete a Sequence from your project, select it and press the Delete key or right-click on it and select **Delete**. This also automatically deletes from your project all Sequences the deleted Sequence may contain.

You can also select multiple Sequences and delete them as a batch.

>**Note:** If you want to delete a Sequence within a Sequence that is a Prefab, you might need to enter the Prefab Mode first. See [how to manage Sequences as Prefabs](sequences-as-prefabs.md).

### Browsing through Sequences

When you select a Sequence:

* The [Sequence Assembly window](sequence-assembly-window.md) displays the properties of the selected Sequence. This is where you can add Sequence Assets to the selected Sequence.

* The [Timeline window](timeline-window.md) displays the tracks contained in the selected Sequence. This is where you can trim and reorder the clips within the selected Sequence.

>**Note:** If you open a Sequence in Prefab Mode, you can only browse through the structure of this Sequence. See [how to manage Sequences as Prefabs](sequences-as-prefabs.md).

### Setting up Scene activation in a Sequence

To create a new Scene and have it activated during the time of a specific Sequence:

1. Right-click on the Sequence, and select Create Scene.
2. Select the location to store the Scene file and specify a name for it.

This action creates an empty Scene that you can further edit. It also creates a Scene Activation track in the selected Sequence in Timeline, and binds the new Scene to this track. Finally, it also additively loads the new Scene in the Hierarchy.

>**Note:** If you want to bind an existing Scene to the track instead of creating a new Scene, you need to create the Scene Activation track from the [Timeline window](timeline-window.md#scene-activation-track).

You can add Scene Activation tracks at any level of your Sequence structure, and you can use as many Scene activation tracks as you need in a single Sequence.

>**Important:** To see a Scene in your Scene and Game views when Unity activates it through a Scene Activation track, you must previously load it in addition to the Scene that contains your Sequences structure. Depending on your current task, you can:
>* [Load Scenes contextually](#loading-scenes-contextually) in Edit mode for any Sequence, directly from the Sequences window.
>* [Set up a Scene Loading Policy](#setting-up-a-scene-loading-policy) to automatically load Scenes in runtime.

### Loading Scenes contextually

If you have set up Scene Activation tracks in your Sequences structure, you can load any of the corresponding Scenes directly from the Sequences window, according to the context of the Sequence you are working on, in Edit mode.

To do this, right-click on the targeted Sequence and select one of the following actions:

| **Action** | **Description** |
|------------|-----------------|
| **Load Scenes** | Additively loads all the Scenes that are bound with a Scene Activation track at any level of the Sequences structure within the time range of the selected Sequence. |
| **Load specific Scene** | Allows you to additively load any specific Scene among the ones that are bound with a Scene Activation track at any level of the Sequences structure within the time range of the selected Sequence. |

### Setting up a Scene Loading Policy

If you have set up Scene Activation tracks in your Sequences structure, you might want Unity to automatically load the corresponding Scenes when you enter the Play mode or when you run a Player build of your project, to ensure that the result includes all of them whatever their current status in Edit mode.

To do this:
1. In the Sequences window, select the Master Sequence.
2. In the Inspector, in the **Sequence Filter** component, click on **Add Scene Loading Policy**.
3. Set up the [Scene Loading Policy](ref-components.md#scene-loading-policy) component according to your needs.

### Recording a Sequence

You can access the Recorder window directly from a selected Sequence. The FPS rate and the Start-and-End frames of the Sequence are automatically applied to any Recorders created for it.  A Recorder can export rendered video, an animation, audio, or a specific render pas.

>**Note:** The minimum version of the Recorder package supported by the Editor you're using is installed automatically when you install Sequences. For current and previous versions of Recorder documentation, see [Unity Recorder](https://docs.unity3d.com/Packages/com.unity.recorder@latest/index.html).<br/>
Depending on the data type you need to export, you may need other packages.

To record a Sequence:
1. Right-click on the targeted Sequence and select **Record**. The Recorder opens with the FPS rate and the Start-and-End frames set to the values that currently delimit the Sequence you selected.
2. On the Recorder window, [add and configure a Recorder](https://docs.unity3d.com/Packages/com.unity.recorder@latest) You can add and configure additional Recorders according to the outputs you want.
3. Manually start the recording.

### Troubleshooting

#### Instantiating a Sequence in the active Scene

If a Sequence structure (a Master Sequence and its Sequences) in the Sequences window is gray because the Scene that contains it was removed or unloaded, you can create an instance of the Sequence structure in the active Scene. You can use this feature to make orphaned Sequences editable and to move Sequences to a different Scene before you add Sequence Assets or other creative content.

>**Note:**
>* The instantiated Sequence structure contains all the Sequences and Timelines in the original Scene, but the structure does not contain any creative content.
>* The instantiated Sequence is independent of other instances of the same Sequence in other Scenes. Changes to one instance of a Sequence have no effect on other instances.

To create an instance of a grayed-out Master Sequence and its Sequences in the active Scene:

In the Sequences window, right-click any Sequence in the grayed-out sequence structure and select **Instantiate in active Scene**. <br />
The Master Sequence and all its child Sequences appear under the active Scene in the Hierarchy window. The Sequences are editable in the Sequences window.

# Known issues and limitations

This page lists some known issues and limitations that you might experience with the Sequences package. It also gives basic instructions to help you work around them when possible.

#### Functionality limitations when converting a Sequence into a Prefab

Unity doesn’t prevent you from converting a Sequence GameObject into a Prefab. However, it is currently not recommended to do it. Such a conversion might bring unwanted side-effects in your workflow, due to the inherent behaviors of Unity Prefabs that are not yet supported in Sequences. For example, you can’t use the Sequence Assembly window to remove Sequence Assets from a Prefab-converted Sequence.

#### Limited Editorial clip manipulation in Timeline

Some Editorial clip manipulations in Timeline are not recommended as they might cause binding loss. For example: moving an Editorial clip from one track to another, or manually removing an Editorial clip from its track.

#### Unexpected suffix in a Sequence name after multiple renamings

If you rename a Sequence multiple times in a row through the Sequences window or the Hierarchy, you might get an unexpected suffix added to the name you typed.

Note that the actual name gets fixed once you add a Sequence Asset to the Sequence.

#### Game view not always updated on Sequence Asset Variant swap

When you swap Variants of a Sequence Asset that is currently framed in the Game view, you might not always be able to see the expected visual result of the swap.

To see the actual result of the Variant swap, you need to slightly scrub the playhead in the Timeline window.

#### Issue when renaming a Sequence Asset with a whitespace

If you rename a Sequence Asset through the Sequence Assembly window, and if the new name includes a whitespace, the Sequence Asset name no longer appears in the Sequence Assembly window.

You can refresh the Sequence Assembly window to fix the display issue, but in that situation, the whitespace is considered as an invalid character, so the Sequence Asset is not renamed.

#### Unexpected Timeline playhead position after creating a Sequence

In certain circumstances after the creation of a Sequence, you might find the Timeline playhead placed at an unexpected position. However, this might not affect your regular workflow.

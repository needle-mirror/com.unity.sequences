# Known issues and limitations

This page lists some known issues and limitations that you might experience with the Sequences package. It also gives basic instructions to help you work around them when possible.

#### Can’t rename a Sequence whose GameObject is a Prefab

**Limitation:** You can’t currently rename a Prefab-converted Sequence from the Sequences window.

**Workaround:** Depending on the scope of your need, use the Hierarchy window to rename the instantiated Prefab, or the Project view to rename the Prefab Asset, or both.

**Note:** if you rename the instantiated Prefab, Unity automatically renames the Sequence in the Sequences window.

#### Limited Editorial clip manipulation in Timeline

Some Editorial clip manipulations in Timeline are not recommended as they might cause binding loss. For example: moving an Editorial clip from one track to another, or manually removing an Editorial clip from its track.

#### Game view not always updated on Sequence Asset Variant swap

When you swap Variants of a Sequence Asset that is currently framed in the Game view, you might not always be able to see the expected visual result of the swap.

To see the actual result of the Variant swap, you need to slightly scrub the playhead in the Timeline window.

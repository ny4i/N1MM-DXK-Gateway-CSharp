# WPF-UI control behaviours, and how they were found

Working notes on `WPF-UI` (`Wpf.Ui`, lepo.co) as used by this program. Every
entry here cost real time to diagnose and would cost it again, because in each
case the XAML reads correctly and the control quietly does something else.

**The one habit that matters:** measure the running window. Reading the markup
found none of these. Screenshots, UI Automation rectangles and pixel sampling
found all of them. If a UI change "looks about right", that is not evidence.

Note also that WPF-UI is **WPF**, not WinUI 3. Advice found online for WinUI 3,
the Windows Community Toolkit or UWP `Expander` frequently does not apply, and
the GitHub issues for those projects describe a different control with a
different template. Check which framework a suggestion is about before acting
on it.

---

## Sections expand and collapse with no animation at all

`ui:CardExpander` does not animate. It shows or hides its content, and the
parent `StackPanel` re-measures once, in a single frame.

**Measured.** Sampling the position of the header below a collapsing section:

```
top = 529   at 1, 43, 70, 106, 131 ms
top = 413   at 169, 194, 221, 258 ms
```

Two positions, 116px apart, nothing in between.

There is no property for this. **Animate the section's own `Height`.** The
mechanism is the layout pass, not the animation: a `StackPanel` re-runs measure
and arrange whenever a child's height changes, so driving that height over
~180ms repositions everything below it on every frame. Nothing has to animate
the siblings; they follow.

Three things about that are counter-intuitive, and each was wrong on the first
attempt. See `MainWindow.AnimateSectionHeight`.

1. **The height cannot be read when `Expanded`/`Collapsed` fires.** `IsExpanded`
   has flipped but the control has not yet shown or hidden its content, so the
   old and new heights come back identical — measured, `from=176 to=176` — and
   nothing animates. Pin the old height immediately instead, which also stops
   the jump reaching the screen, and take the target a dispatcher pass later at
   `DispatcherPriority.Loaded`.

2. **A collapsing section still measures at its EXPANDED height**, even then:
   `184` where `60` was correct. WPF-UI hides the content presenter later than
   any priority that can be waited for without the collapse painting first. Do
   not measure it. A collapsed card is its header, every header is identical,
   so read that height once at startup from a card that begins collapsed — that
   also keeps it correct under theme changes and text scaling.

3. **`DesiredSize` includes the margin; `Height` and `ActualHeight` do not.**
   Animating to the unadjusted figure overshoots by exactly the card's margin
   and snaps back when the height is released. Subtract `Margin.Top` and
   `Margin.Bottom`.

Honour `SystemParameters.ClientAreaAnimation`. Someone who has turned Windows
animations off usually has a reason.

## `BringIntoView` during a layout animation causes a visible bounce

Scrolling a newly opened section into view is reasonable on a short window. Do
it **after** the animation, not during.

**Measured.** Watching the Connection Status card while a section expanded:

| | overshoot past the settled position |
|---|---|
| `BringIntoView` at expand time | **51 px** |
| disabled entirely | 1 px |
| deferred to the animation's completion | **0 px** |

Called mid-flight it forces a layout that shoves unrelated cards down the
window and lets them spring back. That bounce is what reads as "flickering",
and it is easy to blame on the animation.

## `ui:Card` centres its content instead of filling

A `ui:Card` stretched to fill a star-sized row does not give that space to its
content. Any `*` row inside it collapses to the height of its text.

**Measured.** The operation log's `ListBox` was **50px** tall, its heading
stranded in the middle of the card, empty space above and below. Setting
`VerticalContentAlignment="Stretch"` on the card fixed it; the same list is
226px.

Tried and rejected: replacing the card with a plain `Border`, on the theory
that the compiled template had header regions taking room. The measurements
came back byte-identical, so the template was never the problem. Do not repeat
that experiment.

## A keyed `Style` with no `BasedOn` REPLACES the control's template

This is standard WPF, but it is brutal with WPF-UI because the templates carry
the entire Fluent appearance. A keyed `Style` for `ui:ToggleSwitch` without
`BasedOn` rendered the toggles as **bare dots** — no track, no thumb, no
states.

Always:

```xml
<Style x:Key="RowToggle" TargetType="ui:ToggleSwitch"
       BasedOn="{StaticResource {x:Type ui:ToggleSwitch}}">
```

## `ui:HyperlinkButton` ignores `Foreground` set on the control

Its template binds the content's colour to a theme brush. Setting `Foreground`
on the button does nothing.

**Measured** by sampling the pixels: `0,62,110` (hyperlink blue) where
`196,43,28` (red) had been asked for. Put an explicit `TextBlock` in `Content`
and set the colour on that; the pixels then read `196,43,28`.

## `ui:InfoBar` renders `Message` and ignores `Content`

Setting `InfoBar.Message` in code **and** `InfoBar.Content` in markup silently
drops the content. Two buttons existed, were wired up, and were never drawn.
Use one or the other.

## The Fluent scrollbar is an overlay, and padding does not clear it

The horizontal scrollbar is drawn *across* the content rather than as a strip
beside it, so it sits over the newest line of a log pinned to the bottom.

`Padding` on the `ListBox` does **not** fix it: padding is not part of the
scrollable extent, so scrolling to the end still puts the last line under the
bar. Two things are needed together:

- a bottom `Margin` on the **items panel**, which is inside the scroll viewer
  and does count toward the extent;
- `ScrollViewer.ScrollToBottom()` rather than `ScrollIntoView(lastItem)`, which
  only scrolls until the item is just inside the viewport.

---

## How to measure

`MinHeight` reservations, animation smoothness and stray gaps are all easier to
settle with numbers than with opinions. UI Automation gives element rectangles
from outside the process:

```powershell
$main = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
   [System.Windows.Automation.TreeScope]::Children,
   (New-Object System.Windows.Automation.PropertyCondition(
      [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $pid)))
$el = $main.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
   (New-Object System.Windows.Automation.PropertyCondition(
      [System.Windows.Automation.AutomationElement]::NameProperty, "Diagnostics")))
$el.Current.BoundingRectangle
```

Sampling that in a loop while triggering the change shows whether something
animates, and how far it overshoots.

Three traps in doing this, all of which cost time here:

- **`Process.MainWindowHandle` returns NDde's hidden message window** for this
  program — no title, zero rect. Find the real window through UI Automation by
  name instead.
- **A name lookup returns the heading `TextBlock`, not the card.** A "gap"
  measured to a heading includes everything else in that card. One bogus 161px
  figure came from this.
- **`R` is an alias for `Invoke-History`.** Do not name a PowerShell helper
  function `R`.

For colours, sample the PNG rather than trusting your eye at small sizes:
`[System.Drawing.Bitmap]::FromFile(...)` then `GetPixel`.

`PrintWindow` with `PW_RENDERFULLCONTENT` (flag `2`) captures a window that is
partly covered, which matters when the shot is taken on a busy desktop.

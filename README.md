# Charm 
_The high performance C# Overlay Toolkit_

### What is Charm?
Charm is a simple way of drawing overlays and interfacing with an external processes memory.  Simply set up the callback function, and initialize it.

```cs
public static void RenderLoop(Charm.RPM rpm, Charm.Renderer renderer, int width, int height)
{
    renderer.DrawLine(0, 0, width, height, 5, Color.Magenta);
}

static void Main(string[] args)
{
    Charm charm = new Charm();
    charm.CharmInit(RenderLoop, "notepad");
    Console.ReadLine();
}
```

Yep, it's as easy as that.

### Why Charm?
I normally don't condone using C# for these types of purposes, it makes very little sense.  I was prompted to make this after finishing my C++ version, and seeing others still writing overlays with SharpDX and winforms.  My idea was, knowing there are a great many people who won't switch anytime soon to C++, it's better to give them a proper platform purpose built for overlays and external cheats.  I still highly recommend you go and learn C++ over C#, but a toolkit like this will help you to at least 

### What does charm do differently?
Apart from being super simplistic, charm is built for speed.  The overlay itself is ran from an native dll written in C++, with delegates to the managed code to be leveraged by you.  The overlay itself is based on my [D2DOverlay](https://github.com/coltonon/D2DOverlay), with the majority of the code in this project being for Reading/Writing memory.

----

# Getting Started
For some complete code samples, go the the bottom of this document.

You'll need Visual Studio for this.  First, go ahead start a new C# project, I'll simply be using a Console App.

![](https://i.imgur.com/BZCEt3l.png)

First thing we need to do is include the necescary references.

![](https://i.imgur.com/SnDJgvW.png)

From `Assemblies`, we'll first need to add System.Drawing for our colors, and System.Numerics for Vector3s and Matricies.

![](https://i.imgur.com/oriR2vP.png)

Then you're going to want to click `Browse`, then select the `Charm.dll` file you downloaded/compiled.

![](https://i.imgur.com/Nvl3tvi.gif)

Hit `OK`, then lets set up our build options.

![](https://i.imgur.com/9TvPGCv.gif)

The RPM class is built for 64 bit data types.  If you're playing a 32 bit joke of a game, you'll have to rebuild that however you need.

![](https://i.imgur.com/itHJjHF.gif)

Now we can start coding.

First, we'll add our loop to be called once per frame.  Make a static void, with the parameters matching this:
```cs
static void DrawLoop(Charm.RPM rpm, Charm.Renderer renderer, int width, int height)
{

}
```
We'll put all of our drawing calls in here, after we set up Charm.  In your main function, create a new Charm instance.

```cs
static void Main(string[] args)
{
    Charm charm = new Charm();
}
```
Next, we'd optionally set up some settings for the window.  We'll change the font.
```cs
static void Main(string[] args)
{
    Charm charm = new Charm();
    charm.CharmSetOptions(Charm.CharmSettings.CHARM_FONT_IMPACT);
}
```
Any other options you want, you can just OR them together like `setting1 | setting2 | setting3`.

It's already time to initialize the overlay now.  We'll be targetting notepad.

```cs
static void Main(string[] args)
{
    Charm charm = new Charm();
    charm.CharmSetOptions(Charm.CharmSettings.CHARM_FONT_IMPACT);
    charm.CharmInit(DrawLoop, "notepad");
}
```
The first parameter is the name of our drawing loop callback.  The second, is a string name of our process.  The main window of the process will automatically be targetted.

Since the CharmInit function runs in it's own thread, ours will continue running, right on to the end.  We'll add a pause until the user presses enter, to keep the overlay running. `Console.ReadLine();` will do it.

Now to add some things to draw on our window.  In the DrawLoop function, lets draw a box 100 pixels down, 100 pixels to the right, 50 wide and tall, with a red color.

```cs
static void DrawLoop(Charm.RPM rpm, Charm.Renderer renderer, int width, int height)
{
    renderer.DrawBox(100, 100, 50, 50, 0, Color.Red, true);
}
```
Adding `using System.Drawing;` to the top will allow you to use System.Drawing.Color without typing that out every time for the color.

Now lets draw some text at position `10, 10`, with the default size 24.

```cs
renderer.DrawString(10, 20, "Charm Overlay by Coltonon", Color.Green);
```

Open a new notepad window for it to attach to.  Not compile the program, and hit run.

![](https://i.imgur.com/EwsKyfa.png)

Now experiment with drawing some other shapes and lines.

```cs
static void DrawLoop(Charm.RPM rpm, Charm.Renderer renderer, int width, int height)
{
    renderer.DrawBox(100, 100, 50, 50, 0, Color.Red, true);
    renderer.DrawString(10, 20, "Charm Overlay by Coltonon", Color.Green);
    renderer.DrawLine(0, height - 100, 100, height, 3, Color.Blue);
    renderer.DrawLine(width - 100, 0, width, 100, 3, Color.Blue);
    renderer.DrawCircle(125, 125, 25, 0, Color.Yellow, true);
    renderer.DrawEllipse(width * .6f, height * .5f, width * .2f, height/3, 5, Color.Purple, false);
}
```

![](https://i.imgur.com/yNjo0p7.png)

Full code is available at the bottom, plus an example using RPM.




----

# Documentation
# `Charm`

#### `Charm(void)` 
(ctor) The construcor is responsible for writing the native dll to disk for the pinvokes to work.

#### `CharmSetOptions(CharmSettings)`
 The available options for the window you can set.  They can be OR'ed together.  Available options include:

```
CHARM_REQUIRE_FOREGROUND
CHARM_DRAW_FPS
CHARM_VSYNC
CHARM_FONT_CALIBRI
CHARM_FONT_ARIAL
CHARM_FONT_COURIER
CHARM_FONT_GABRIOLA
CHARM_FONT_IMPACT
WPM_WRITE_DIRTY
```
`CharmSettings.CHARM_REQUIRE_FOREGROUND` Requires the targetted window to be in the foreground in order for it to render.

`CharmSettings.CHARM_DRAW_FPS` Draws the FPS of the overlay in the top right corner.  FPS is roughly calculated based on frametime, and should not be taken as accurate during high framerates.

`CharmSettings.CHARM_VSYNC` Attempts to lock the framerate at 60fps, to save resources.  Doesn't work properly at 300+ framerates, as it's using the same flawed framerate calculation.

`CharmSettings.CHARM_FONT_CALIBRI` Sets the text font to Calibri, if it's installed.

`CharmSettings.CHARM_FONT_COURIER` Sets the text font to Courier, if it's installed.

`CharmSettings.CHARM_FONT_GABRIOLA` Sets the text font to Gabriola, if it's installed.

`CharmSettings.CHARM_FONT_IMPACT` Sets the text font to Impact bold, if it's installed.

`CharmSettings.WPM_WRITE_DIRTY` Rather than using WriteProcessMemory, in order to skip querying memory before writing, this skips the bs and uses NtWriteVirtualMemory.  This can potentially save calling 3 winapis, but if the desired memory to be written is protected, the memory write will fail.

#### `CharmResult CharmInit(CallbackManaged, string)`
Gets everything running.  The first parameter is the callback, or the function where all of your stuff is to be rendered.  The string is the name of the **process**, not the window.  Return value is of type CharmResult, documentation for below. Declare a function like such:

```cs
public static void RenderLoop(Charm.RPM rpm, Charm.Renderer renderer, int width, int height)
```
CharmResult:
```
CHARM_SUCCESS,      // no errors
CHARM_PROCESS_NONE, // targetted process not found
CHARM_PROCESS_MANY, // multiple instances of targetted process found
CHARM_NATIVE_NONE,  // the native dll isn't found
CHARM_WINDOW_NONE   // the main window of the targetted process doesn't exist 
```


# `Renderer` class
To be used only within the rendering loop callback, this class gives you methods for drawing on the screen, as well as WorldToScreen functionality.

#### `DrawLine(float x1, float y1, float x2, float y2, float thickness, System.Drawing.Color color)`
Draws a 2d line from `x1, y1` to `x2, y2` with the specified thickness and color.

#### `DrawBox(float x, float y, float width, float height, float thickness, System.Drawing.Color color, bool filled)`
Draws a box on the screen.  Width and height are relative to the x & y coordinates.  `filled` determines if the box is solid, or an outline.  If `filled` is true, `thickness` is ignored.

#### `DrawCircle(float x, float y, float radius, float thickness, System.Drawing.Color color, bool filled)`
Draws a circle with the specified radius.  

#### `DrawEllipse(float x, float y, float width, float height, float thickness, System.Drawing.Color color, bool filled)`
Draws an ellipse with the specified width and height radii.

#### `DrawString(float x, float y, string text, System.Drawing.Color color, float fontsize)`
Draws a string.  Supports unicode, default font size is 24, which is quite large.

#### `SetViewProjection(System.Numerics.Matrix4x4)`
In order to use WorldToScreen calculations, you must set up the view projection matrix.  Width and height of the screen are already taken care of, simply pass a System.Numerics.Matrix4x4 every time the camera changes positions.

#### `WorldToScreen(System.Numerics.Vector3 m_World, out System.Numerics.Vector3 m_Screen)`
Granted that you've called `SetViewProjection`, it'll use that matrix to provide translate a world-space coordinate to a screen-space coordinate.



# `RPM` class
Used for reading and writing memory.

#### `ReadStruct<Generic>(long addr)`
Reads a struct at the given address.  **Use this instead of the below**.  Example for a struct to use:
```cs
[StructLayout(LayoutKind.Explicit)]
public struct PlayerManager
{
    [FieldOffset(0x568)]
    public long localPlayer;
}
```
Using this method allows you to read an entire class at a time, as opposed to reading each member indivdually.  **This is a huge factor in performance.**

#### `ReadInt64`
Reads a 64-bit integer, used for reading pointers.

#### `ReadInt32` 
Reads a 32-bit integer.

#### `ReadFloat`
Reads a floating-point integer.

#### `ReadByte`
Reads a single byte from memory.

#### `ReadString`
Reads a string from memory.  Max buffer size is 512, it'll automatically split it at the first null character.

#### `ReadVector3`
Reads a System.Numerics.Vector3.

#### `ReadMatrix`
Reads a System.Numerics.Matrix4x4

#### `WriteMemory(long addr, byte[] Buffer)`
Input an address and a buffer of memory to write, and it'll write it for you.  Great if you have some custom types to marshal, or if you're writing shellcode.  All memory writes will use `WriteProcessMemory` or `NtWriteVirtualMemory` based on if you used `CharmSettings.WPM_WRITE_DIRTY` before calling `CharmInit`.

#### `WriteFloat`
Writes a floating point integer to memory.

#### `WriteInt32` 
Writes a standard int32.

#### `WriteInt64`
Writes a 64-bit integer, the size and encoding of pointers.

#### `WriteString`
Writes a string to memory, using ASCII encoding.

#### `WriteByte` 
Writes a single byte to memory.

### `IsValid(long)`
Uses VirtualQueryEx to determine if the inputted address is indeed committed memory.  Avoid over-using, as it is an api call that potentially takes time.


----


# Complete Examples

## Demo 1
Complete code from the _Getting Started_ walkthrough.

```cs
using System;
using System.Drawing;

namespace Charm_Getting_Started
{
    class Program
    {
        static void Main(string[] args)
        {
            Charm charm = new Charm();
            charm.CharmSetOptions(Charm.CharmSettings.CHARM_FONT_IMPACT);
            charm.CharmInit(DrawLoop, "notepad");
            Console.ReadLine();
        }

        static void DrawLoop(Charm.RPM rpm, Charm.Renderer renderer, int width, int height)
        {
            renderer.DrawBox(100, 100, 50, 50, 0, Color.Red, true);
            renderer.DrawString(10, 20, "Charm Overlay by Coltonon", Color.Green);
            renderer.DrawLine(0, height - 100, 100, height, 3, Color.Blue);
            renderer.DrawLine(width - 100, 0, width, 100, 3, Color.Blue);
            renderer.DrawCircle(125, 125, 25, 0, Color.Yellow, true);
            renderer.DrawEllipse(width * .6f, height * .5f, width * .2f, height/3, 5, Color.Purple, false);
        }
    }
}
```


## Demo 2
Attaches to the game Star Wars Battlefront 2, reads it's view projection matrix, and renders some stats gathered from memory.

```cs
using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Charm_Test
{
    class Program
    {
        // this is the function that'll be called once per overlay frame
        public static void RenderLoop(Charm.RPM rpm, Charm.Renderer renderer, int width, int height)
        {
            // --------------------------------------------  Demo 1 -----------------------------------
            // objective: read the player's name and stats, then draw them on the screen.

            var pGameContext = rpm.ReadStruct<Offsets.GameContext>(rpm.ReadInt64(Offsets.StaticOffsets.OFFSET_GAMECONTEXT)); // double deref
            var pPlayerManager = rpm.ReadStruct<Offsets.PlayerManager>(pGameContext.playerManager);
            var pLocalPlayer = rpm.ReadStruct<Offsets.ClientPlayer>(pPlayerManager.localPlayer);

            string name = rpm.ReadString(pLocalPlayer.name);

            int textHeight = 25; // we'll increment this programatically rather than hardcoding it
            renderer.DrawString(20, textHeight += 25, name, Color.AliceBlue, 30);

            // each member of the ClientPlayer class has already been read, so there's no need to call RPM more than once
            renderer.DrawString(20, textHeight += 25, "Score: " + pLocalPlayer.score, Color.BlueViolet);
            renderer.DrawString(20, textHeight += 25, "Kills: " + pLocalPlayer.kills, Color.Lime);
            renderer.DrawString(20, textHeight += 25, "Deaths: " + pLocalPlayer.deaths, Color.OrangeRed);

            // --------------------------------------------  Demo 2 -----------------------------------

            // Read the view projection matrix. 
            // no need for making a struct for the first level pointer, there are no other members and it's offest is 0.
            var pGameRenderer = rpm.ReadStruct<Offsets.GameRenderer>(rpm.ReadInt64(Offsets.StaticOffsets.OFFSET_GAMERENDERER));
            
            // return now if the view projection isn't valid
            if (!rpm.IsValid(pGameRenderer.renderView)) return;

            // Cannot marshal a System.Numerics.Matrix4x4 from our offsets structs, so we get the address of pGameRenderer.renderView + 0x430, which is what that marshalling offsetof should do.
            Matrix4x4 pViewProj = rpm.ReadMatrix(pGameRenderer.renderView + (long)Marshal.OffsetOf<Offsets.RenderView>("viewProjection"));

            // set the view projection for WorldToScreen to work
            renderer.SetViewProjection(pViewProj);

            // make a random in-game coordinate (nowhere in particular)
            Vector3 worldPosition = new Vector3(0, 80, 0);

            // convert game coordinate to screen coordinate, and check if it's valid
            if (renderer.WorldToScreen(worldPosition, out Vector3 screenPosition))
            {
                // draw a line from the top of the screen to our world coordinate
                renderer.DrawLine(width / 2, 0, screenPosition.X, screenPosition.Y, 5, Color.Yellow);

                // draw some text at that coordinate
                renderer.DrawString(screenPosition.X, screenPosition.Y, "Text in the sky", Color.Lime);
            }
        }

        static void Main(string[] args)
        {
            // initialize a new Charm instance
            Charm charm = new Charm();

            // Make the overlay only render when the taget's window is active, and draw the fps
            charm.CharmSetOptions(Charm.CharmSettings.CHARM_REQUIRE_FOREGROUND | Charm.CharmSettings.CHARM_DRAW_FPS);

            // initialize the overlay, with our callback function above, and the name of the game to adhere to
            charm.CharmInit(RenderLoop, "starwarsbattlefrontii");

            // it runs in it's own thread, so we can continue doing stuff here, or just wait for the user to exit.
            Console.ReadLine();
        }
    }
}
```

The above demo requires these offsets:

```cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Charm_Test
{
    public struct Offsets
    {
        public struct StaticOffsets
        {
            public static long OFFSET_GAMECONTEXT = 0x144311e50;
            public static long OFFSET_GAMERENDERER = 0x1445323b0;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct GameRenderer
        {
            [FieldOffset(0x538)]
            public long renderView;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct RenderView
        {
            // while we can't marshal this member (it's a matrix4x4), it's better to use a marshal.offsetof calculation than hardcode all offsets
            [FieldOffset(0x430)]
            public long viewProjection;
        }


        [StructLayout(LayoutKind.Explicit)]
        public struct GameContext
        {
            [FieldOffset(0x58)]
            public long playerManager;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct PlayerManager
        {
            [FieldOffset(0x568)]
            public long localPlayer;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct ClientPlayer
        {
            [FieldOffset(0x18)]
            public long name;

            [FieldOffset(0x144C)]
            public int score;

            [FieldOffset(0x1454)]
            public int kills;

            [FieldOffset(0x1458)]
            public int assists;

            [FieldOffset(0x145C)]
            public int deaths;

        }
    }
}
```
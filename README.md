# WindowsFormsRender3D
Sample WinForms application to render simple mesh in 3D with camera rotation.

### Highlights

A **`Mesh`** class contains nodes and elements to define shapes. Each element (face) defines its own color. Each mesh has a poistion and orientation property.
A **`Scene`** class contains multiple meshes. A **`Camera`** class targets a specific WinForms `Control` for render and applies perspective and orientation transformation.
The camera `.Paint(Camera c, Graphics g)` event must be subscribed to and should call `Scene.Render(c,g);` to display the scene into the control. 
A `Timer` object advances the frames in the windows form.

### Screenshot

![scr](https://github.com/ja72/WindowsFormsRender3D/blob/master/Images/2022-06-18_16_49_58-3D%20Scene%20Render.png)

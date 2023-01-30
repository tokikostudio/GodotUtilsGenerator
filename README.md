Godot Utils Generator
===
A C# Incremental Source Generator used to parse `project.godot` file, and generate various compile-time constant which are kept in sync with the project settings, thus removing the need for raw strings or manual hardcoded values.

**IMPORTANT: The generator is based on Godot 4.x version, and rely on the separation between 2D and 3D class name.**

- [Installation](#installation)
- [Usage](#usage)
- [Licence](#licence)

---
## Installation

- Clone or download the project
- Update your Godot `Project.csproj` file `PropertyGroup` and `ProjectReference` sections as shown below:
```csproj
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>

<ItemGroup>
  <ProjectReference 
    Include="PathTo\GodotInputNameGenerator.csproj"
    OutputItemType="Analyzer"
    ReferenceOutputAssembly="false"/>
    
  <AdditionalFiles Include="project.godot"/>
</ItemGroup>
```
- Rebuild the solution and you're done 🥳

---
## Usage

### Inputs

Input Actions names will be generated in the `GodotExtensions.InputName` class.

![image](https://user-images.githubusercontent.com/1193295/214432075-331cee78-6d6d-47e8-8c20-c4c2651d49b8.png)
```cs 
using GodotUtils;

float walk = Input.GetAxis(InputName.MoveLeft, InputName.MoveRight);
```

### Layers

Physics, Render and Navigation layers bitmask value will be generated in the `GodotExtensions.Layer` class: it contains each combinations of `2D` and `3D` for each `Render`, `Physics` and `Navigation` layer.

Extensions classes are also generated for 2D and 3D variant of `VisualInstance`, `CollisionObject`, `NavigationLink`, `NavigationRegion` and `NavigationAgent` Godot classes.

Usage sample:

![image](https://user-images.githubusercontent.com/1193295/214432327-313d020c-d313-46cd-bf77-68233f5c946d.png)

```cs
using GodotUtils;


uint collisionMask = Layer.Physics2D.Enemy | Layer.Physics2D.Props;
    
private void OnArea2DEntered(Area2D area)
{
    if (area.HasCollisionLayerEnemy())
        // ...
}
```

### NodePath with BindExport

The `BindExport(T)` attribute is used to generate a corresponding field with type `T` and have it already setup through one function call. The attribute takes a `Type` constructor parameter so it can be used in the generic GetNode Godot function.

**IMPORTANT:** It requires the exported NodePath field name to ends with `NodePath` 

Instead of the old way of writing nodepath and associated fields:
```cs
using GodotUtils;
public partial MyNode : Node
{
  [Export] private NodePath _labelANodePath;
  [Export] private NodePath _labelBNodePath;
  [Export] private NodePath _timerNodePath;
  [Export] private NodePath _otherNodeNodePath;
  
  private Label _labelA;
  private Label _labelB;
  private Timer _timer;
  private Node otherNode;
  public override void _EnterTree()
  {
    _labelA = GetNode<Label>(_labelANodePath);
    _labelB = GetNode<Label>(_labelBNodePath);
    _timer = GetNode<Timer>(_timerNodePath);
    _otherNode = GetNode<Node>(_otherNodeNodePath);
  }
}
```

We can focus on just declaring the NodePath, the binding type and voilà!

```cs
using GodotUtils;
public partial MyNode : Node
{
  [Export, BindExport(typeof(Label))] private NodePath _labelANodePath;
  [Export, BindExport(typeof(Label))] private NodePath _labelBNodePath;
  [Export, BindExport(typeof(Timer))] private NodePath _timerNodePath;
  [Export, BindExport(typeof(Node))]  private NodePath _otherNodeNodePath;
  
  public override void _EnterTree()
  {
    BindExportedNodePaths();
    // You can start using _labelA, _labelB, _timer and _otherNode
  }
}
```

---
## Licence

Licenced under the MIT licence, see [LICENCE.txt](https://github.com/tokikostudio/GodotUtilsGenerator/blob/main/LICENSE) for more information.


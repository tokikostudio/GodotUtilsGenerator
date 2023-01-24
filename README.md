Godot Utils Generator
===
A C# Incremental Source Generator used to parse `project.godot` file,
and generate various compile-time constant which are kept in sync with the project settings,
thus removing the need for raw strings or manual hardcoded values.

- [Installation](#installation)
- [Usage](#usage)
- [Licence](#licence)

---
### Installation

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
### Usage

#### Inputs

[Input Actions](https://docs.godotengine.org/en/stable/tutorials/inputs/input_examples.html#inputmap) names will be generated in the `GodotExtensions.InputName` class.

```diff 
using GodotExtensions;

- float walk = Input.GetAxis("move_left", "move_right");
+ float walk = Input.GetAxis(InputName.MoveLeft, InputName.MoveRight);
```

#### Layers

[Physics Layer](https://docs.godotengine.org/en/stable/tutorials/physics/physics_introduction.html#collision-layers-and-masks) bitmask value will be generated in the `GodotExtensions.LayerName` class.

It contains each combinations of `2D` and `3D` for each `Render`, `Physics` and `Navigation` layer.

Usage sample:
```cs

    private void OnArea3DEntered(Area3D area)
    {
        if ((area.CollisionLayer & LayerName.Physics3D.Player) != 0)
            ; // ...

        if ((area.CollisionMask & LayerName.Physics3D.Enemy) != 0)
            ; // ...
    }
```
---
### Licence

Licenced under the MIT licence, see [LICENCE.txt](https://github.com/tokikostudio/GodotInputNameGenerator/blob/main/LICENSE) for more information.


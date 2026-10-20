namespace Sight.Serialization.Benchmarks;

/// <summary>
/// Deterministic benchmark data
/// </summary>
public static class TestData
{
    private static readonly string[] s_meshes = { "rock.mesh", "tree.mesh", "house.mesh", "crate.mesh", "lamp.mesh" };
    private static readonly string[] s_materials = { "stone", "wood", "metal", "glass" };

    public static Person CreatePerson()
    {
        return new Person
        {
            Name = "Ada Lovelace",
            Age = 36,
            Email = "ada.lovelace@example.com",
            Score = 98.25,
            IsActive = true,
            CreatedAt = 1_700_000_000_000,
            Tags = new List<string> { "admin", "beta", "verified" },
        };
    }

    public static SampleSet CreateSampleSet(int count = 10_000)
    {
        var random = new Random(42);
        var set = new SampleSet { Source = "sensor-01", Samples = new List<Sample>(count) };
        var timestamp = 1_700_000_000_000L;
        for (var i = 0; i < count; i++)
        {
            timestamp += random.Next(1, 100);
            set.Samples.Add(new Sample
            {
                Id = i,
                X = (float)(random.NextDouble() * 100),
                Y = (float)(random.NextDouble() * 100),
                Z = (float)(random.NextDouble() * 10),
                Timestamp = timestamp,
                Quality = (SampleQuality)random.Next(3),
            });
        }

        return set;
    }

    /// <summary>
    /// Scene of groups containing meshes and lights; every child references its parent group
    /// </summary>
    public static Scene CreateScene(int groups = 50, int childrenPerGroup = 20)
    {
        var random = new Random(42);
        var scene = new Scene { Name = "Level 1" };
        for (var g = 0; g < groups; g++)
        {
            var group = new GroupEntity { Name = "group" + g, Position = RandomVector(random) };
            scene.Entities.Add(group);
            for (var c = 0; c < childrenPerGroup; c++)
            {
                Entity child = c % 5 == 0
                    ? new LightEntity { Intensity = (float)random.NextDouble(), Color = (uint)random.Next() }
                    : new MeshEntity { Mesh = s_meshes[random.Next(s_meshes.Length)], Material = s_materials[random.Next(s_materials.Length)] };

                child.Name = group.Name + "/child" + c;
                child.Position = RandomVector(random);
                child.Parent = group;
                group.Children.Add(child);
                scene.Entities.Add(child);
            }
        }

        return scene;
    }

    private static Vector3 RandomVector(Random random)
    {
        return new Vector3 { X = (float)random.NextDouble() * 100, Y = (float)random.NextDouble() * 100, Z = (float)random.NextDouble() * 100 };
    }
}

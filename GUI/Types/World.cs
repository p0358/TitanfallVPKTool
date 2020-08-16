﻿using System;
using System.Globalization;
using GUI.Utils;
using OpenTK;
using ValvePak;
using ValveResourceFormat;
using ValveResourceFormat.Blocks;
using ValveResourceFormat.ResourceTypes;
using ValveResourceFormat.ResourceTypes.NTROSerialization;
using Vector3 = OpenTK.Vector3;
using Vector4 = OpenTK.Vector4;

namespace GUI.Types
{
    internal class World
    {
        private readonly Resource Resource;

        private static int anonymousCameraCount;

        public World(Resource resource)
        {
            Resource = resource;
        }

        internal void AddObjects(Renderer.Renderer renderer, string path, Package package)
        {
            var data = Resource.Blocks[BlockType.DATA] as NTRO;

            // Output is World_t we need to iterate m_worldNodes inside it.
            var worldNodes = (NTROArray)data.Output["m_worldNodes"];
            if (worldNodes.Count > 0)
            {
                var nodeData = ((NTROValue<NTROStruct>)worldNodes[0]).Value; //TODO: Not be 0.

                var worldNode = ((NTROValue<string>)nodeData["m_worldNodePrefix"]).Value;
                if (worldNode != null)
                {
                    var newResource = FileExtensions.LoadFileByAnyMeansNecessary(worldNode + ".vwnod_c", path, package);
                    if (newResource == null)
                    {
                        Console.WriteLine("unable to load model " + worldNode + ".vwnod_c");
                        throw new Exception("WTF");
                    }

                    var node = new WorldNode(newResource);
                    node.AddMeshes(renderer, path, package);
                }
            }

            var entityLumps = (NTROArray)data.Output["m_entityLumps"];
            foreach (var lump in entityLumps)
            {
                LoadEntities(lump, renderer, path, package);
            }
        }

        private static void LoadEntities(NTROValue lump, Renderer.Renderer renderer, string path, Package package)
        {
            var reference = ((NTROValue<ResourceExtRefList.ResourceReferenceInfo>)lump).Value;
            if (reference == null)
            {
                return;
            }

            var newResource = FileExtensions.LoadFileByAnyMeansNecessary(reference.Name + "_c", path, package);
            if (newResource == null)
            {
                Console.WriteLine("unable to load entity lump " + reference.Name + "_c");

                return;
            }

            var entityLump = newResource.Blocks[BlockType.DATA] as EntityLump;

            var childLumps = (NTROArray)entityLump.Output["m_childLumps"];

            foreach (var lump2 in childLumps)
            {
                var lol = ((NTROValue<ResourceExtRefList.ResourceReferenceInfo>)lump).Value;

                // TODO: Should be controlled in UI with world layers
                if (lol.Name.Contains("_destruction"))
                {
                    continue;
                }

                LoadEntities(lump2, renderer, path, package);
            }

            foreach (var entity in entityLump.Datas)
            {
                var scale = string.Empty;
                var position = string.Empty;
                var angles = string.Empty;
                var model = string.Empty;
                var skin = string.Empty;
                var colour = new byte[0];
                var classname = string.Empty;
                var name = string.Empty;

                foreach (var property in entity)
                {
                    //metadata
                    switch (property.Item2)
                    {
                        case 3368008710: //World Model
                            model = property.Item3 as string;
                            break;
                        case 3827302934: //Position
                            position = property.Item3 as string;
                            break;
                        case 3130579663: //Angles
                            angles = property.Item3 as string;
                            break;
                        case 432137260: //Scale
                            scale = property.Item3 as string;
                            break;
                        case 2020856412: //Skin
                            skin = property.Item3 as string;
                            break;
                        case 588463423: //Colour
                            colour = property.Item3 as byte[];
                            break;
                        case 3323665506: //Classname
                            classname = property.Item3 as string;
                            break;
                        case 1094168427:
                            name = property.Item3 as string;
                            break;
                    }
                }

                if (scale == string.Empty || position == string.Empty || angles == string.Empty)
                {
                    continue;
                }

                var isCamera =
                    classname == "info_player_start" ||
                    classname == "worldspawn" ||
                    classname == "sky_camera" ||
                    classname == "point_devshot_camera" ||
                    classname == "point_camera";

                if (!isCamera && model == string.Empty)
                {
                    continue;
                }

                var scaleMatrix = Matrix4.CreateScale(ParseCoordinates(scale));

                var positionVector = ParseCoordinates(position);
                var positionMatrix = Matrix4.CreateTranslation(positionVector);

                var pitchYawRoll = ParseCoordinates(angles);
                var rollMatrix = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(pitchYawRoll.Z)); // Roll
                var pitchMatrix = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(pitchYawRoll.X)); // Pitch
                var yawMatrix = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(pitchYawRoll.Y)); // Yaw

                var rotationMatrix = rollMatrix * pitchMatrix * yawMatrix;
                var transformationMatrix = scaleMatrix * rotationMatrix * positionMatrix;

                if (isCamera)
                {
                    if (classname == "worldspawn")
                    {
                        renderer.SetDefaultWorldCamera(positionVector);
                    }
                    else
                    {
                        renderer.AddCamera(name == string.Empty ? $"{classname} #{anonymousCameraCount++}" : name, transformationMatrix);
                    }

                    continue;
                }

                var objColor = Vector4.One;

                // Parse colour if present
                if (colour.Length == 4)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        objColor[i] = colour[i] / 255.0f;
                    }
                }

                var newEntity = FileExtensions.LoadFileByAnyMeansNecessary(model + "_c", path, package);
                if (newEntity == null)
                {
                    Console.WriteLine($"unable to load entity {model}_c");

                    continue;
                }

                var entityModel = new Model(newEntity);
                entityModel.LoadMeshes(renderer, path, transformationMatrix, objColor, package, skin);
            }
        }

        private static Vector3 ParseCoordinates(string input)
        {
            var vector = default(Vector3);
            var split = input.Split(' ');

            for (var i = 0; i < split.Length; i++)
            {
                vector[i] = float.Parse(split[i], CultureInfo.InvariantCulture);
            }

            return vector;
        }
    }
}

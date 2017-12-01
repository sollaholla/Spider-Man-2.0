using GTA;
using GTA.Math;
using GTA.Native;
using System.Collections.Generic;
using System;

namespace spiderman.net.Library
{
    /// <summary>
    /// The type of rope to create.
    /// </summary>
    public enum GTARopeType
    {
        ThickRope = 4,
        MetalWire = 5
    }

    /// <summary>
    /// The rope class that properly implements methods used to manipulate ropes.
    /// </summary>
    public class GTARope : IHandleable
    {
        /// <summary>
        /// A bool that acts as a helper to determine 
        /// whether or not this rope is winding.
        /// </summary>
        private bool _winding;

        /// <summary>
        /// Our main constructor.
        /// </summary>
        /// <param name="handle"></param>
        public GTARope(int handle)
        {
            Handle = handle;
        }

        /// <summary>
        /// The handle of the rope, or it's index in the game's internal array.
        /// </summary>
        public int Handle { get; internal set; }

        /// <summary>
        /// Get's the coordinate of the vertex at 'index'.
        /// </summary>
        /// <param name="index">The index of the vertex.</param>
        /// <returns></returns>
        public Vector3 this[int index] {
            get {
                return Function.Call<Vector3>(Hash.GET_ROPE_VERTEX_COORD, Handle, index);
            }
        }

        /// <summary>
        /// Get's whether or not this rope is winding (true after "StartRopeWinding" is called).
        /// </summary>
        public bool IsWinding {
            get {
                return _winding;
            }
        }

        /// <summary>
        /// Get's or set's the length of this rope.
        /// </summary>
        public float Length {
            get {
                return Function.Call<float>(Hash._GET_ROPE_LENGTH, Handle);
            }
            set {
                Function.Call(Hash.ROPE_FORCE_LENGTH, value);
                Function.Call(Hash.ROPE_RESET_LENGTH, Length);
            }
        }

        /// <summary>
        /// Toggle's shadows on / off.
        /// </summary>
        public bool UseShadows {
            set {
                unsafe
                {
                    int handle = Handle;
                    Function.Call(Hash.ROPE_DRAW_SHADOW_ENABLED, &handle, value);
                    Handle = handle;
                }
            }
        }

        /// <summary>
        /// Get's the vertex count for this rope.
        /// </summary>
        public int VertexCount {
            get {
                return Function.Call<int>(Hash.GET_ROPE_VERTEX_COUNT, Handle);
            }
        }

        /// <summary>
        /// Pins a vertex to the specified coordinate.
        /// </summary>
        /// <param name="vertex">The index of the vertex.</param>
        /// <param name="coord">The coordinate.</param>
        public void PinVertex(int vertex, Vector3 coord)
        {
            if (vertex >= VertexCount)
                return;

            Function.Call(Hash.PIN_ROPE_VERTEX, Handle, vertex, coord.X, coord.Y, coord.Z);
            UpdatePinnedVerticies();
        }

        /// <summary>
        /// Detach the specified entity from this rope.
        /// </summary>
        /// <param name="entity">The entity to detach the rope from.</param>
        public void DetachEntity(Entity entity)
        {
            Function.Call(Hash.DETACH_ROPE_FROM_ENTITY, Handle, entity.Handle);
        }

        /// <summary>
        /// Forces the pineed verticies to update their positions.
        /// </summary>
        public void UpdatePinnedVerticies()
        {
            Function.Call(Hash.ROPE_SET_UPDATE_PINVERTS, Handle);
        }

        /// <summary>
        /// Unpins a pinnded vertex.
        /// </summary>
        /// <param name="vertex">The index of the pinned vertex.</param>
        public void UnpinVertex(int vertex)
        {
            if (vertex >= VertexCount)
                return;

            Function.Call(Hash.UNPIN_ROPE_VERTEX, Handle, vertex);
        }

        /// <summary>
        /// Activates physics for this rope.
        /// </summary>
        public void ActivatePhysics()
        {
            Function.Call(Hash.ACTIVATE_PHYSICS, Handle);
        }

        /// <summary>
        /// Attaches two entities to this rope.
        /// </summary>
        /// <param name="entity1">The first entity to attach.</param>
        /// <param name="entity1Offset">The offset from the first entity.</param>
        /// <param name="entity2">The second entity to attach.</param>
        /// <param name="entity2Offset">The offset from the second entity.</param>
        /// <param name="length">The desired length of the rope.</param>
        public void AttachEntities(Entity entity1, Vector3 entity1Offset, Entity entity2, Vector3 entity2Offset, float length,
            string bone1 = "", string bone2 = "")
        {
            var offset1 = entity1.Position + entity1Offset;
            var offset2 = entity2.Position + entity2Offset;

            Function.Call(Hash.ATTACH_ENTITIES_TO_ROPE, Handle, entity1.Handle, entity2.Handle,
                offset1.X, offset1.Y, offset1.Z,
                offset2.X, offset2.Y, offset2.Z,
                length, false, false, bone1, bone2);
        }

        /// <summary>
        /// Get's the coordinate of the last vertex.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetLastVertexCoord()
        {
            return Function.Call<Vector3>(Hash.GET_ROPE_LAST_VERTEX_COORD, Handle);
        }

        /// <summary>
        /// Get's all the vertex coordinates and stores it into an array.
        /// </summary>
        /// <returns></returns>
        public Vector3[] ToArray()
        {
            var vList = new List<Vector3>();

            // Loop through each vertex.
            for (int i = 0; i < VertexCount; i++)
            {
                // Get the vertex position.
                var pos = this[i];

                // Add it to the list.
                vList.Add(pos);
            }
            // return the list as an array.
            return vList.ToArray();
        }

        /// <summary>
        /// Start's winding this rope.
        /// </summary>
        public void StartWinding()
        {
            Function.Call(Hash.START_ROPE_WINDING, Handle);
            Function.Call(Hash.START_ROPE_UNWINDING_FRONT, Handle);
            _winding = true;
        }

        /// <summary>
        /// Converts this rope to essentially a straight line.
        /// </summary>
        public void ConvertToSimple()
        {
            Function.Call(Hash.ROPE_CONVERT_TO_SIMPLE, Handle);
        }

        /// <summary>
        /// Stops the winding of this rope.
        /// </summary>
        public void StopWinding()
        {
            Function.Call(Hash.STOP_ROPE_WINDING, Handle);
            Function.Call(Hash.STOP_ROPE_UNWINDING_FRONT, Handle);
            _winding = false;
        }

        /// <summary>
        /// Deletes this rope.
        /// </summary>
        public void Delete()
        {
            unsafe
            {
                int handle = Handle;
                Function.Call(Hash._0x52B4829281364649, &handle);
                Handle = handle;
            }
        }

        /// <summary>
        /// Check if this rope exists.
        /// </summary>
        /// <returns></returns>
        public bool Exists()
        {
            unsafe
            {
                int handle = Handle;
                var exists = Function.Call<bool>(Hash.DOES_ROPE_EXIST, &handle);
                Handle = handle;
                return exists;
            }
        }

        /// <summary>
        /// Add a new rope into the game.
        /// </summary>
        /// <param name="startPosition">The starting position of the first vertex coordinate.</param>
        /// <param name="initialLength">The initial length of the rope.</param>
        /// <param name="type">The type of rope to create.</param>
        /// <param name="maximumLength">The maximum length of this rope. Usually the same as the initial length.</param>
        /// <param name="minimumLength">The minimum length of this rope. The rope will never go under this length.</param>
        /// <param name="rigid">Whether or not this rope starts out with physics enabled.</param>
        /// <param name="breakWhenShot">Whether or not this rope can be broken by firearms.</param>
        /// <returns></returns>
        public static GTARope AddRope(Vector3 startPosition, float initialLength, GTARopeType type,
            float maximumLength, float minimumLength, bool rigid, bool breakWhenShot)
        {
            LoadTextures();

            unsafe
            {
                int ptr = 0;
                return new GTARope(Function.Call<int>(Hash.ADD_ROPE,
                    startPosition.X, startPosition.Y, startPosition.Z,
                    0f, 0f, 0f,
                    initialLength,
                    (int)type,
                    maximumLength,
                    minimumLength,
                    0f,
                    false, false, false,
                    5.0f,
                    breakWhenShot, &ptr));
            }
        }

        /// <summary>
        /// Unloads rope textures from the game.
        /// </summary>
        public static void UnloadTextures()
        {
            if (Function.Call<bool>(Hash.ROPE_ARE_TEXTURES_LOADED))
            {
                Function.Call(Hash.ROPE_UNLOAD_TEXTURES);
            }
        }

        /// <summary>
        /// Loads the rope textures into the game if they don't exist.
        /// </summary>
        public static void LoadTextures()
        {
            if (!Function.Call<bool>(Hash.ROPE_ARE_TEXTURES_LOADED))
            {
                Function.Call(Hash.ROPE_LOAD_TEXTURES);
                while (!Function.Call<bool>(Hash.ROPE_ARE_TEXTURES_LOADED))
                {
                    Script.Yield();
                }
            }
        }

        /// <summary>
        /// Returns true if this rope is not null,
        /// and exists in game memory.
        /// </summary>
        /// <param name="rope"></param>
        /// <returns></returns>
        public static bool Exists(GTARope rope)
        {
            return rope != null && rope.Exists();
        }
    }
}

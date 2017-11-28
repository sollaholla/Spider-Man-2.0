﻿using GTA;
using GTA.Math;
using spiderman.net.Library.Extensions;
using Rope = spiderman.net.Library.Rope;

namespace spiderman.net.Abilities.WebAttachment
{
    public class AttachmentInfo
    {
        public AttachmentInfo(Entity entity1, Entity entity2, Rope rope)
        {
            Entity1 = entity1;
            Entity2 = entity2;
            Rope = rope;
        }

        public Entity Entity1 { get; }
        public Entity Entity2 { get; }
        public Rope Rope { get; }
        public bool Terminated { get; private set; }

        public void ProcessAttachment(Vector3 referenceCoords)
        {
            if (!Rope.Exists() || Terminated)
                return;

            const float MaxDistance = 7000f;
            float distance1 = Vector3.DistanceSquared(referenceCoords, Entity1.Position);
            float distance2 = Vector3.DistanceSquared(referenceCoords, Entity2.Position);

            Entity1.IsPersistent = true;
            Entity2.IsPersistent = true;

            if ((distance1 >= MaxDistance || distance2 >= MaxDistance) ||
                (!Entity.Exists(Entity1) || !Entity.Exists(Entity2)))
            {
                Delete();
                Terminated = true;
            }
        }

        public void Delete()
        {
            Rope.DetachEntity(Entity1);
            Rope.DetachEntity(Entity2);

            if (Entity1.IsPersistent)
                Entity1.MarkAsNoLongerNeeded();
            if (Entity2.IsPersistent)
                Entity2.MarkAsNoLongerNeeded();

            ResetEntityRagdoll(Entity1);
            ResetEntityRagdoll(Entity2);

            Rope.Delete();
        }

        private void ResetEntityRagdoll(Entity entity)
        {
            if (entity.GetEntityType() == EntityType.Ped)
            {
                var p = new Ped(entity.Handle);
                p.Euphoria.StopAllBehaviours.Start(0);
                p.Task.ClearAll();
            }
        }
    }
}

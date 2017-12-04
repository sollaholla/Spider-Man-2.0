using GTA;
using GTA.Math;
using SpiderMan.Library.Extensions;
using Rope = SpiderMan.Library.Types.Rope;

namespace SpiderMan.Abilities.Types
{
    public class AttachmentInfo
    {
        public AttachmentInfo(Entity entity1, Entity entity2, Rope rope)
        {
            Entity1 = entity1;
            Entity1.IsPersistent = true;
            Entity2 = entity2;
            Entity2.IsPersistent = true;
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

            const float maxDistance = 7000f;
            var distance1 = Vector3.DistanceSquared(referenceCoords, Entity1.Position);
            var distance2 = Vector3.DistanceSquared(referenceCoords, Entity2.Position);

            if (distance1 >= maxDistance)
            {
                Delete();
                Terminated = true;
                return;
            }

            if (distance2 >= maxDistance)
            {
                Delete();
                Terminated = true;
                return;
            }

            if (!Entity.Exists(Entity1))
            {
                Delete();
                Terminated = true;
                return;
            }

            if (!Entity.Exists(Entity2))
            {
                Delete();
                Terminated = true;
                return;
            }

            if (!Entity1.IsPersistent)
            {
                Delete();
                Terminated = true;
                return;
            }

            if (Entity2.IsPersistent) return;
            Delete();
            Terminated = true;
        }

        public void Delete()
        {
            Rope.DetachEntity(Entity1);
            Rope.DetachEntity(Entity2);

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
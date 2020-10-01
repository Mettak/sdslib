using sdslib.ResourceTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sdslib
{
    public class ResourceList<T> : List<T> where T : Resource
    {
        public new void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Add(T resource, List<ResourceType> resourceTypes)
        {
            if (resourceTypes == null)
            {
                throw new ArgumentNullException(nameof(resourceTypes));
            }

            if (resource.GetType().BaseType != typeof(Resource))
            {
                throw new Exception("Cannot add base type with this function.");
            }

            string typeName = resource.GetType().Name;

            var type = resourceTypes.FirstOrDefault(x => x.ToString() == typeName);
            resource.Info.Type = type ?? throw new Exception($"Resource type {typeName} not found");
            base.Add(resource);
        }
    }
}

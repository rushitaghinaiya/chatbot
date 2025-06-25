using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VRMDBCommon2023
{
    public static class ClassUtilities
    {
        /// <summary>
        /// It will copy class A properties into class B by ignoring case
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="child"></param>
        public static void CopyClassIgnorecase(object parent, object child)
        {
            var parentProperties = parent.GetType().GetProperties();
            var childProperties = child.GetType().GetProperties();

            foreach (var parentProperty in parentProperties)
            {
                foreach (var childProperty in childProperties)
                {
                    if (parentProperty.Name.Equals(childProperty.Name, StringComparison.OrdinalIgnoreCase) && parentProperty.PropertyType == childProperty.PropertyType)
                    {
                        childProperty.SetValue(child, parentProperty.GetValue(parent));
                        break;
                    }
                }
            }
        }

        public static void CopyProperties(this object source, object destination)
        {
            // If any this null throw an exception
            if (source == null || destination == null)
                throw new Exception("Source or/and Destination Objects are null");
            // Getting the Types of the objects
            Type typeDest = destination.GetType();
            Type typeSrc = source.GetType();

            // Iterate the Properties of the source instance and  
            // populate them from their desination counterparts  
            PropertyInfo[] srcProps = typeSrc.GetProperties();
            foreach (PropertyInfo srcProp in srcProps)
            {
                if (!srcProp.CanRead)
                {
                    continue;
                }
                PropertyInfo targetProperty = typeDest.GetProperty(srcProp.Name);
                if (targetProperty == null)
                {
                    continue;
                }
                if (!targetProperty.CanWrite)
                {
                    continue;
                }
                if (targetProperty.GetSetMethod(true) != null && targetProperty.GetSetMethod(true).IsPrivate)
                {
                    continue;
                }
                if ((targetProperty.GetSetMethod().Attributes & MethodAttributes.Static) != 0)
                {
                    continue;
                }
                if (!targetProperty.PropertyType.IsAssignableFrom(srcProp.PropertyType))
                {
                    continue;
                }
                // Passed all tests, lets set the value
                targetProperty.SetValue(destination, srcProp.GetValue(source, null), null);
            }
        }

        //public static void CopyPropertiesWithList<TSource, TDestination>(TSource source, TDestination destination)
        //{
        //    PropertyInfo[] sourceProperties = typeof(TSource).GetProperties();
        //    PropertyInfo[] destinationProperties = typeof(TDestination).GetProperties();

        //    foreach (PropertyInfo sourceProperty in sourceProperties)
        //    {
        //        if (sourceProperty.CanRead)
        //        {
        //            PropertyInfo destinationProperty = destinationProperties.FirstOrDefault(x =>
        //                x.Name.Equals(sourceProperty.Name, StringComparison.OrdinalIgnoreCase) &&
        //                x.PropertyType == sourceProperty.PropertyType);

        //            if (destinationProperty != null && destinationProperty.CanWrite)
        //            {
        //                if (sourceProperty.PropertyType.IsGenericType &&
        //                    sourceProperty.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
        //                {
        //                    Type sourcelistType = sourceProperty.PropertyType.GetGenericArguments()[0];
        //                    Type destlistType = destinationProperty.PropertyType.GetGenericArguments()[0];
        //                    MethodInfo copyMethod = typeof(ClassUtilities).GetMethod("CopyPropertiesWithList", BindingFlags.Public | BindingFlags.Static)
        //                        .MakeGenericMethod(sourcelistType,destlistType);

        //                    object sourceValue = sourceProperty.GetValue(source);
        //                    object destinationValue = Activator.CreateInstance(sourceProperty.PropertyType);

        //                    copyMethod.Invoke(null, new object[] { sourceValue, destinationValue });

        //                    destinationProperty.SetValue(destination, destinationValue);
        //                }
        //                else
        //                {
        //                    object sourceValue = sourceProperty.GetValue(source);
        //                    destinationProperty.SetValue(destination, sourceValue);
        //                }
        //            }
        //        }
        //    }
        //}

        public static void CopyPropertiesWithList<TSource, TDestination>(TSource source, TDestination destination)
        {
            PropertyInfo[] sourceProperties = typeof(TSource).GetProperties();
            PropertyInfo[] destinationProperties = typeof(TDestination).GetProperties();

            foreach (PropertyInfo sourceProperty in sourceProperties)
            {
                if (sourceProperty.CanRead)
                {
                    PropertyInfo destinationProperty = destinationProperties.FirstOrDefault(x =>
                        x.Name.Equals(sourceProperty.Name, StringComparison.OrdinalIgnoreCase) &&
                        x.PropertyType == sourceProperty.PropertyType);

                    if (destinationProperty != null && destinationProperty.CanWrite)
                    {
                        object sourceValue = sourceProperty.GetValue(source);

                        if (sourceValue is IList<object> sourceList)
                        {
                            Type listType = sourceProperty.PropertyType.GetGenericArguments()[0];

                            if (destinationProperty.GetValue(destination) is IList<object> destinationList)
                            {
                                destinationList.Clear(); // Clear existing items in the destination list

                                foreach (var item in sourceList)
                                {
                                    var destinationItem = Activator.CreateInstance(listType);
                                    CopyPropertiesWithList(item, destinationItem);
                                    destinationList.Add(destinationItem);
                                }
                            }
                        }
                        else
                        {
                            object destinationValue = sourceProperty.GetValue(source);
                            destinationProperty.SetValue(destination, destinationValue);
                        }
                    }
                }
            }
        }
    }
}


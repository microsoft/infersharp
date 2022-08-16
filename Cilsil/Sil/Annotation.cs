// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Collections.Generic;

namespace Cilsil.Sil
{
    /// <summary>
    /// Smallfoot Intermediate Language representation of annotations, a type of metadata that can
    /// be added to source code.
    /// </summary>
    public class Annotation
    {
        /// <summary>
        /// The name of the annotation.
        /// </summary>
        public string ClassName { get; }

        /// <summary>
        /// The parameters of the annotation. Currently expect only singleton iterables.
        /// </summary>
        public IEnumerable<string> Params { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Annotation"/> class.
        /// </summary>
        /// <param name="className">The name of the annotation.</param>
        /// <param name="parameters">The parameters of the annotation. Currently expect only 
        /// singleton iterables.</param>
        public Annotation(string className, IEnumerable<string> parameters)
        {
            ClassName = className;
            Params = parameters;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => $"{ClassName}, [{string.Join(", ", Params)}]";
    }

    /// <summary>
    /// Annotation for one type, which is a list of source code annotations with their visibility.
    /// </summary>
    public class ItemAnnotation
    {
        /// <summary>
        /// The source code annotations.
        /// </summary>
        public List<ItemAnnotationEntry> Annotations { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemAnnotation"/> class.
        /// </summary>
        /// <param name="annotations">The annotations.</param>
        public ItemAnnotation(List<ItemAnnotationEntry> annotations = null)
        {
            Annotations = annotations ?? new List<ItemAnnotationEntry>();
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() 
            => $"[{string.Join(", ", Annotations.ConvertAll(x => x.ToString()))}]";

        /// <summary>
        /// An individual source code annotation, with visibility.
        /// </summary>
        public class ItemAnnotationEntry
        {
            /// <summary>
            /// The source code annotation.
            /// </summary>
            public Annotation Annotation { get; }

            /// <summary>
            /// <c>true</c> if <see cref="ItemAnnotationEntry"/> is visible; <c>false</c>
            /// otherwise.
            /// </summary>
            public bool Visible { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="ItemAnnotationEntry"/> class.
            /// </summary>
            /// <param name="annotation">The source code annotation.</param>
            /// <param name="visible"><c>true</c> if <see cref="ItemAnnotationEntry"/> is visible; 
            /// <c>false</c> otherwise.</param>
            public ItemAnnotationEntry(Annotation annotation, bool visible)
            {
                Annotation = annotation;
                Visible = visible;
            }

            /// <summary>
            /// Converts to string.
            /// </summary>
            /// <returns>
            /// A <see cref="string" /> that represents this instance.
            /// </returns>
            public override string ToString() => $"{Annotation} : {Visible}";
        }
    }

    /// <summary>
    /// Annotation for a method: return value and list of parameters.
    /// </summary>
    public class MethodAnnotation
    {
        /// <summary>
        /// The return value.
        /// </summary>
        public ItemAnnotation ReturnValue { get; }

        /// <summary>
        /// The parameters.
        /// </summary>
        public List<ItemAnnotation> Params { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodAnnotation"/> class.
        /// </summary>
        /// <param name="returnValue">The return value</param>
        /// <param name="parameters">The parameters; not presently used in frontend.</param>
        public MethodAnnotation(ItemAnnotation returnValue = null,
                                List<ItemAnnotation> parameters = null)
        {
            ReturnValue = returnValue ?? new ItemAnnotation();
            Params = parameters ?? new List<ItemAnnotation>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="className"></param>
        public void AddAnnotationNoParameter(string className)
        {
            var annotation = new Annotation(className, new List<string>());
            var itemAnnotationEntry = new ItemAnnotation.ItemAnnotationEntry(annotation, true);
            ReturnValue.Annotations.Add(itemAnnotationEntry);
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => 
            $"{ReturnValue}";
    }
}

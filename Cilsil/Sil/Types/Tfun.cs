using Mono.Cecil;
using Newtonsoft.Json;

namespace Cilsil.Sil.Types
{
    /// <summary>
    /// Represents to a function type.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Tfun : Typ
    {
        /// <summary>
        /// The method underlying this type.
        /// </summary>
        public MethodReference Method;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tfun"/> class.
        /// </summary>
        /// <param name="method">The method underlying this type.</param>
        public Tfun(MethodReference method)
        {
            Method = method;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => "_fun_";
        
        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this 
        /// instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this 
        /// instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; 
        ///   otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj) => obj is Tfun;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data 
        /// structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => ToString().GetHashCode();
    }
}
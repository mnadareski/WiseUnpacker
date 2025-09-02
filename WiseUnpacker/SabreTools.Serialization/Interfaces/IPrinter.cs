using System.Text;

namespace SabreTools.Serialization.Interfaces
{
    /// <summary>
    /// Marks a class as a printer associated with a model
    /// </summary>
    /// <typeparam name="TModel">Type of the top-level model</typeparam>
    public interface IPrinter<TModel>
    {
        /// <summary>
        /// Print information associated with a model
        /// </summary>
        /// <param name="builder">StringBuilder to append information to</param>
        /// <param name="model">Model to print</param>
        void PrintInformation(StringBuilder builder, TModel model);
    }
}

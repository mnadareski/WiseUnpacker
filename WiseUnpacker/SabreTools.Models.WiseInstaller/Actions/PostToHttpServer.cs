namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Post to HTTP Server
    /// 
    /// This action posts information over the Internet to a Web server. (Example: Use it to
    /// record user registration information or other data.) You must set up a CGI program or
    /// Active Server Page (.ASP) on the server that accepts data sent by an HTTP POST
    /// operation and deciphers encoded characters.
    /// 
    /// The destination computer must have a valid Internet connection. If end users might not
    /// have this capability, you can add a prompt on a dialog box asking the end user if they
    /// have Internet connectivity. Then use the results from the prompt to run this action or
    /// not.
    /// </summary>
    /// <remarks>
    /// This action is called through Call DLL Function and is mapped to "f34".
    /// This acts like the start of a block if a flag is set.
    /// </remarks>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class PostToHttpServer
    {
        /// <summary>
        /// Flags from the argument data
        /// </summary>
        /// <remarks>
        /// Encoded as a string, binary representation in script file.
        /// Expected flags:
        /// - Ignore Errors (unknown)
        /// - Abort Installation (unknown)
        /// - Start Block (unknown)
        /// </remarks>
        public byte DataFlags { get; set; }

        /// <summary>
        /// URL
        /// </summary>
        public string? URL { get; set; }

        /// <summary>
        /// Text to post in "field=data" format
        /// </summary>
        public string? PostData { get; set; }
    }
}
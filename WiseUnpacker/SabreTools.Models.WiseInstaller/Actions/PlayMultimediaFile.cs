namespace SabreTools.Models.WiseInstaller.Actions
{
    /// <summary>
    /// Play Multimedia File
    /// 
    /// This action plays an audio (.WAV) or video (.AVI) file during installation. Playback is
    /// asynchronous, which means the sound or movie can play while the installation
    /// continues. The multimedia file must be installed on the destination computer before this
    /// action is called. It must be small enough to fit into the destination computer’s RAM for it
    /// to play correctly, because the disk is heavily accessed by the installation process. To
    /// produce sound, the destination computer must be properly equipped and configured.
    /// </summary>
    /// <see href="https://codeberg.org/CYBERDEV/REWise/src/branch/master/src/wisescript.h"/>
    /// <see href="https://www.manualslib.com/manual/404969/Symantec-Wisescript-Editor-8-0-Reference-For-Wise-Package-Studio-V1-0.html"/> 
    public class PlayMultimediaFile : MachineStateData
    {
        /// <summary>
        /// Flags
        /// </summary>
        /// Values:
        /// - Loop Continuously (0x01)
        /// - File Type (WAV or AVI) (0x02 == AVI)
        /// </remarks>
        public byte Flags { get; set; }

        /// <summary>
        /// X Position on a 640 x 480 screen for AVI to play
        /// </summary>
        public ushort XPosition { get; set; }

        /// <summary>
        /// Y Position on a 640 x 480 screen for AVI to play
        /// </summary>
        public ushort YPosition { get; set; }

        /// <summary>
        /// Path to the .WAV or .AVI file
        /// </summary>
        public string? Pathname { get; set; }
    }
}

using System.Text;
using SabreTools.Models.WiseInstaller;
using SabreTools.Models.WiseInstaller.Actions;
using SabreTools.Serialization.Interfaces;

namespace SabreTools.Serialization.Printers
{
    public class WiseScript : IPrinter<ScriptFile>
    {
        /// <inheritdoc/>
        public void PrintInformation(StringBuilder builder, ScriptFile model)
            => Print(builder, model);

        public static void Print(StringBuilder builder, ScriptFile scriptFile)
        {
            builder.AppendLine("Wise Installer Script File Information:");
            builder.AppendLine("-------------------------");
            builder.AppendLine();

            Print(builder, scriptFile.Header);
            Print(builder, scriptFile.States);
        }

        private static void Print(StringBuilder builder, ScriptHeader? header)
        {
            builder.AppendLine("  Header Information:");
            builder.AppendLine("  -------------------------");
            if (header == null)
            {
                builder.AppendLine("  No header");
                builder.AppendLine();
                return;
            }

            builder.AppendLine(header.Flags, "  Flags");
            builder.AppendLine(header.UnknownU16_1, "  UnknownU16_1");
            builder.AppendLine(header.UnknownU16_2, "  UnknownU16_2");
            builder.AppendLine(header.SomeOffset1, "  SomeOffset1");
            builder.AppendLine(header.SomeOffset2, "  SomeOffset2");
            builder.AppendLine(header.UnknownBytes_2, "  UnknownBytes_2");
            builder.AppendLine(header.DateTime, "  Datetime");
            builder.AppendLine(header.VariableLengthData, "  Variable length data");
            builder.AppendLine(header.FTPURL, "  FTP URL");
            builder.AppendLine(header.LogPathname, "  Log pathname");
            builder.AppendLine(header.MessageFont, "  Font");
            builder.AppendLine(header.FontSize, "  Font size");
            builder.AppendLine(header.Unknown_2, "  Unknown_2");
            builder.AppendLine(header.LanguageCount, "  Language count");
            builder.AppendLine();
            builder.AppendLine("  Header strings");
            builder.AppendLine("  -------------------------");
            if (header.HeaderStrings == null || header.HeaderStrings.Length == 0)
            {
                builder.AppendLine("  No header strings");
            }
            else
            {
                for (int i = 0; i < header.HeaderStrings.Length; i++)
                {
                    var entry = header.HeaderStrings[i];
                    builder.AppendLine($"  Header String {i}: {entry}");
                }
            }
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, MachineState[]? entries)
        {
            builder.AppendLine("  State Machine Information:");
            builder.AppendLine("  -------------------------");
            if (entries == null || entries.Length == 0)
            {
                builder.AppendLine("  No state machine items");
                return;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];

                builder.AppendLine($"  State Machine Entry {i}:");
                builder.AppendLine($"    Op: {entry.Op} (0x{(byte)entry.Op:X2})");
                switch (entry.Data)
                {
                    case InstallFile data: Print(builder, data); break;
                    case NoOp data: Print(builder, data); break;
                    case DisplayMessage data: Print(builder, data); break;
                    case UserDefinedActionStep data: Print(builder, data); break;
                    case EditIniFile data: Print(builder, data); break;
                    case DisplayBillboard data: Print(builder, data); break;
                    case ExecuteProgram data: Print(builder, data); break;
                    case EndBlockStatement data: Print(builder, data); break;
                    case CallDllFunction data: Print(builder, data); break;
                    case EditRegistry data: Print(builder, data); break;
                    case DeleteFile data: Print(builder, data); break;
                    case IfWhileStatement data: Print(builder, data); break;
                    case ElseStatement data: Print(builder, data); break;
                    case StartUserDefinedAction data: Print(builder, data); break;
                    case EndUserDefinedAction data: Print(builder, data); break;
                    case CreateDirectory data: Print(builder, data); break;
                    case CopyLocalFile data: Print(builder, data); break;
                    case CustomDialogSet data: Print(builder, data); break;
                    case GetSystemInformation data: Print(builder, data); break;
                    case GetTemporaryFilename data: Print(builder, data); break;
                    case PlayMultimediaFile data: Print(builder, data); break;
                    case NewEvent data: Print(builder, data); break;
                    case Unknown0x19 data: Print(builder, data); break;
                    case ConfigODBCDataSource data: Print(builder, data); break;
                    case IncludeScript data: Print(builder, data); break;
                    case AddTextToInstallLog data: Print(builder, data); break;
                    case RenameFileDirectory data: Print(builder, data); break;
                    case OpenCloseInstallLog data: Print(builder, data); break;
                    case ElseIfStatement data: Print(builder, data); break;
                    case Unknown0x24 data: Print(builder, data); break;
                    case Unknown0x25 data: Print(builder, data); break;

                    // Should never happen
                    case InvalidOperation data: Print(builder, data); break;
                    default: builder.AppendLine("    Data: [NULL]"); break;
                }
            }
        }

        #region State Actions

        private static void Print(StringBuilder builder, InstallFile data)
        {
            builder.AppendLine($"    Data: InstallFile");
            builder.AppendLine(data.Flags, $"      Flags");
            builder.AppendLine(data.DeflateStart, $"      Deflate start");
            builder.AppendLine(data.DeflateEnd, $"      Deflate end");
            builder.AppendLine(data.Date, $"      Date");
            builder.AppendLine(data.Time, $"      Time");
            builder.AppendLine(data.InflatedSize, $"      Inflated size");
            builder.AppendLine(data.Operand_7, $"      Unknown");
            builder.AppendLine(data.Crc32, $"      CRC-32");
            builder.AppendLine(data.DestinationPathname, $"      Destination pathname");
            builder.AppendLine($"      File texts");
            builder.AppendLine("      -------------------------");
            if (data.Description == null || data.Description.Length == 0)
            {
                builder.AppendLine("      No file texts");
            }
            else
            {
                for (int i = 0; i < data.Description.Length; i++)
                {
                    var entry = data.Description[i];
                    builder.AppendLine($"      File Text {i}: {entry}");
                }
            }
            builder.AppendLine(data.Source, $"      Source");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, NoOp data)
        {
            builder.AppendLine($"    Data: NoOp");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, DisplayMessage data)
        {
            builder.AppendLine($"    Data: DisplayMessage");
            builder.AppendLine(data.Flags, $"      Flags");
            builder.AppendLine($"      Title/Text strings");
            builder.AppendLine("      -------------------------");
            if (data.TitleText == null || data.TitleText.Length == 0)
            {
                builder.AppendLine("      No title/text strings");
            }
            else
            {
                for (int i = 0; i < data.TitleText.Length; i++)
                {
                    var entry = data.TitleText[i];
                    builder.AppendLine($"      Title/Text String {i}: {entry}");
                }
            }
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, UserDefinedActionStep data)
        {
            builder.AppendLine($"    Data: UserDefinedActionStep");
            builder.AppendLine(data.Flags, $"      Count");
            builder.AppendLine($"      Script lines");
            builder.AppendLine("      -------------------------");
            if (data.ScriptLines == null || data.ScriptLines.Length == 0)
            {
                builder.AppendLine("      No script lines");
            }
            else
            {
                for (int i = 0; i < data.ScriptLines.Length; i++)
                {
                    var entry = data.ScriptLines[i];
                    builder.AppendLine($"      Script Line {i}: {entry}");
                }
            }
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, EditIniFile data)
        {
            builder.AppendLine($"    Data: EditIniFile");
            builder.AppendLine(data.Pathname, $"      Pathname");
            builder.AppendLine(data.Section, $"      Section");
            builder.AppendLine(data.Values, $"      Values");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, DisplayBillboard data)
        {
            builder.AppendLine($"    Data: DisplayBillboard");
            builder.AppendLine(data.Flags, $"      Flags");

            builder.AppendLine($"      Deflate info:");
            builder.AppendLine($"      -------------------------");
            if (data.DeflateInfo == null || data.DeflateInfo.Length == 0)
            {
                builder.AppendLine("  No deflate info items");
            }
            else
            {
                for (int i = 0; i < data.DeflateInfo.Length; i++)
                {
                    var entry = data.DeflateInfo[i];
                    Print(builder, entry, 8, i);
                }
            }

            builder.AppendLine(data.Terminator, $"      Terminator");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ExecuteProgram data)
        {
            builder.AppendLine($"    Data: ExecuteProgram");
            builder.AppendLine(data.Flags, $"      Flags");
            builder.AppendLine(data.Pathname, $"      Pathname");
            builder.AppendLine(data.CommandLine, $"      Command Line");
            builder.AppendLine(data.DefaultDirectory, $"      Default Directory");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, EndBlockStatement data)
        {
            builder.AppendLine($"    Data: EndBlockStatement");
            builder.AppendLine(data.Operand_1, $"      Operand 1");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, CallDllFunction data)
        {
            builder.AppendLine($"    Data: CallDllFunction");
            builder.AppendLine(data.Flags, $"      Flags");
            builder.AppendLine(data.DllPath, $"      DLL path");
            builder.AppendLine(data.FunctionName, $"      Function name");
            builder.AppendLine(data.FunctionName.FromWiseFunctionId(), $"      Derived action name");
            builder.AppendLine(data.Operand_4, $"      Operand 4");
            builder.AppendLine(data.ReturnVariable, $"      Return variable");
            builder.AppendLine($"      Entries");
            builder.AppendLine("      -------------------------");
            if (data.Entries == null || data.Entries.Length == 0)
            {
                builder.AppendLine("      No entry data");
            }
            else
            {
                for (int i = 0; i < data.Entries.Length; i++)
                {
                    var entry = data.Entries[i];
                    switch (entry)
                    {
                        case AddDirectoryToPath args: Print(builder, args, i); break;
                        case AddToAutoexecBat args: Print(builder, args, i); break;
                        case AddToConfigSys args: Print(builder, args, i); break;
                        case AddToSystemIni args: Print(builder, args, i); break;
                        case ReadIniValue args: Print(builder, args, i); break;
                        case GetRegistryKeyValue args: Print(builder, args, i); break;
                        case RegisterFont args: Print(builder, args, i); break;
                        case Win32SystemDirectory args: Print(builder, args, i); break;
                        case CheckConfiguration args: Print(builder, args, i); break;
                        case SearchForFile args: Print(builder, args, i); break;
                        case ReadWriteBinaryFile args: Print(builder, args, i); break;
                        case SetVariable args: Print(builder, args, i); break;
                        case GetEnvironmentVariable args: Print(builder, args, i); break;
                        case CheckIfFileDirExists args: Print(builder, args, i); break;
                        case SetFileAttributes args: Print(builder, args, i); break;
                        case SetFilesBuffers args: Print(builder, args, i); break;
                        case FindFileInPath args: Print(builder, args, i); break;
                        case CheckDiskSpace args: Print(builder, args, i); break;
                        case InsertLineIntoTextFile args: Print(builder, args, i); break;
                        case ParseString args: Print(builder, args, i); break;
                        case ExitInstallation args: Print(builder, args, i); break;
                        case SelfRegisterOCXsDLLs args: Print(builder, args, i); break;
                        case InstallDirectXComponents args: Print(builder, args, i); break;
                        case WizardBlockLoop args: Print(builder, args, i); break;
                        case ReadUpdateTextFile args: Print(builder, args, i); break;
                        case PostToHttpServer args: Print(builder, args, i); break;
                        case PromptForFilename args: Print(builder, args, i); break;
                        case StartStopService args: Print(builder, args, i); break;
                        case CheckHttpConnection args: Print(builder, args, i); break;
                        case ExternalDllCall args: Print(builder, args, i); break;

                        // Should never happen
                        default: builder.AppendLine($"      Entry {i}: [NULL]"); break;
                    }
                }
            }
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, EditRegistry data)
        {
            builder.AppendLine($"    Data: EditRegistry");
            builder.AppendLine(data.FlagsAndRoot, $"      Flags and root");
            builder.AppendLine(data.DataType, $"      Data type");
            builder.AppendLine(data.UnknownFsllib, $"      Unknown");
            builder.AppendLine(data.Key, $"      Key");
            builder.AppendLine(data.NewValue, $"      New value");
            builder.AppendLine(data.ValueName, $"      Value name");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, DeleteFile data)
        {
            builder.AppendLine($"    Data: DeleteFile");
            builder.AppendLine(data.Flags, $"      Flags");
            builder.AppendLine(data.Pathname, $"      Pathname");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, IfWhileStatement data)
        {
            builder.AppendLine($"    Data: IfWhileStatement");
            builder.AppendLine(data.Flags, $"      Flags");
            builder.AppendLine(data.Variable, $"      Variable");
            builder.AppendLine(data.Value, $"      Value");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ElseStatement data)
        {
            builder.AppendLine($"    Data: ElseStatement");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, StartUserDefinedAction data)
        {
            builder.AppendLine($"    Data: StartUserDefinedAction");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, EndUserDefinedAction data)
        {
            builder.AppendLine($"    Data: EndUserDefinedAction");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, CreateDirectory data)
        {
            builder.AppendLine($"    Data: CreateDirectory");
            builder.AppendLine(data.Pathname, $"      Pathname");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, CopyLocalFile data)
        {
            builder.AppendLine($"    Data: CopyLocalFile");
            builder.AppendLine(data.Flags, $"      Flags");
            builder.AppendLine(data.Padding, $"      Padding");
            builder.AppendLine(data.Destination, $"      Destination");
            builder.AppendLine($"      Descriptions");
            builder.AppendLine("      -------------------------");
            if (data.Description == null || data.Description.Length == 0)
            {
                builder.AppendLine("      No descriptions");
            }
            else
            {
                for (int i = 0; i < data.Description.Length; i++)
                {
                    var entry = data.Description[i];
                    builder.AppendLine($"      Description {i}: {entry}");
                }
            }
            builder.AppendLine();
            builder.AppendLine(data.Source, $"      Source");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, CustomDialogSet data)
        {
            builder.AppendLine($"    Data: CustomDialogSet");
            builder.AppendLine(data.DeflateStart, $"      Deflate start");
            builder.AppendLine(data.DeflateEnd, $"      Deflate end");
            builder.AppendLine(data.InflatedSize, $"      Inflated size");
            builder.AppendLine(data.DisplayVariable, $"      Display variable");
            builder.AppendLine(data.Name, $"      Name");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, GetSystemInformation data)
        {
            builder.AppendLine($"    Data: GetSystemInformation");
            builder.AppendLine(data.Flags, $"      Variable");
            builder.AppendLine(data.Variable, $"      Variable");
            builder.AppendLine(data.Pathname, $"      Pathname");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, GetTemporaryFilename data)
        {
            builder.AppendLine($"    Data: GetTemporaryFilename");
            builder.AppendLine(data.Variable, $"      Variable");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, PlayMultimediaFile data)
        {
            builder.AppendLine($"    Data: PlayMultimediaFile");
            builder.AppendLine(data.Flags, $"      Flags");
            builder.AppendLine(data.XPosition, $"      X position");
            builder.AppendLine(data.YPosition, $"      Y position");
            builder.AppendLine(data.Pathname, $"      Pathname");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, NewEvent data)
        {
            builder.AppendLine($"    Data: NewEvent");
            builder.AppendLine(data.Padding, $"      Padding");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, Unknown0x19 data)
        {
            builder.AppendLine($"    Data: Unknown0x19");
            builder.AppendLine(data.Operand_1, $"      Unknown");
            builder.AppendLine(data.Operand_2, $"      Unknown");
            builder.AppendLine(data.Operand_3, $"      Unknown");
            builder.AppendLine(data.Operand_4, $"      Unknown");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ConfigODBCDataSource data)
        {
            builder.AppendLine($"    Data: ConfigODBCDataSource");
            builder.AppendLine(data.Flags, $"      Flags");
            builder.AppendLine(data.FileFormat, $"      File format");
            builder.AppendLine(data.ConnectionString, $"      Connection string");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, IncludeScript data)
        {
            builder.AppendLine($"    Data: IncludeScript");
            builder.AppendLine(data.Count, $"      Count");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, AddTextToInstallLog data)
        {
            builder.AppendLine($"    Data: AddTextToInstallLog");
            builder.AppendLine(data.Text, $"      Text");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, RenameFileDirectory data)
        {
            builder.AppendLine($"    Data: RenameFileDirectory");
            builder.AppendLine(data.OldPathname, $"      Old pathname");
            builder.AppendLine(data.NewFileName, $"      New file name");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, OpenCloseInstallLog data)
        {
            builder.AppendLine($"    Data: OpenCloseInstallLog");
            builder.AppendLine(data.Flags, $"      Flags");
            builder.AppendLine(data.LogName, $"      Log name");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ElseIfStatement data)
        {
            builder.AppendLine($"    Data: ElseIfStatement");
            builder.AppendLine(data.Operator, $"      Operator");
            builder.AppendLine(data.Variable, $"      Variable");
            builder.AppendLine(data.Value, $"      Value");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, Unknown0x24 data)
        {
            builder.AppendLine($"    Data: Unknown0x24");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, Unknown0x25 data)
        {
            builder.AppendLine($"    Data: Unknown0x25");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, InvalidOperation data)
        {
            builder.AppendLine($"    Data: InvalidOperation");
            builder.AppendLine();
        }

        #endregion

        #region Function Actions

        private static void Print(StringBuilder builder, AddDirectoryToPath data, int i)
        {
            builder.AppendLine($"      Entry {i}: AddDirectoryToPath");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.Directory, $"        Directory");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, AddToAutoexecBat data, int i)
        {
            builder.AppendLine($"      Entry {i}: AddToAutoexecBat");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.FileToEdit, $"        File to edit");
            builder.AppendLine(data.TextToInsert, $"        Text to insert");
            builder.AppendLine(data.SearchForText, $"        Search for text");
            builder.AppendLine(data.CommentText, $"        Comment text");
            builder.AppendLine(data.LineNumber, $"        Line number");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, AddToConfigSys data, int i)
        {
            builder.AppendLine($"      Entry {i}: AddToConfigSys");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.FileToEdit, $"        File to edit");
            builder.AppendLine(data.TextToInsert, $"        Text to insert");
            builder.AppendLine(data.SearchForText, $"        Search for text");
            builder.AppendLine(data.CommentText, $"        Comment text");
            builder.AppendLine(data.LineNumber, $"        Line number");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, AddToSystemIni data, int i)
        {
            builder.AppendLine($"      Entry {i}: AddToSystemIni");
            builder.AppendLine(data.DeviceName, $"        Device name");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ReadIniValue data, int i)
        {
            builder.AppendLine($"      Entry {i}: ReadIniValue");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.Variable, $"        Variable");
            builder.AppendLine(data.Pathname, $"        Pathname");
            builder.AppendLine(data.Section, $"        Section");
            builder.AppendLine(data.Item, $"        Item");
            builder.AppendLine(data.DefaultValue, $"        Default value");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, GetRegistryKeyValue data, int i)
        {
            builder.AppendLine($"      Entry {i}: GetRegistryKeyValue");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.Variable, $"        Variable");
            builder.AppendLine(data.Key, $"        Key");
            builder.AppendLine(data.Default, $"        Default");
            builder.AppendLine(data.ValueName, $"        Value name");
            builder.AppendLine(data.Root, $"        Root");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, RegisterFont data, int i)
        {
            builder.AppendLine($"      Entry {i}: RegisterFont");
            builder.AppendLine(data.FontFileName, $"        Font file name");
            builder.AppendLine(data.FontName, $"        Font name");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, Win32SystemDirectory data, int i)
        {
            builder.AppendLine($"      Entry {i}: Win32SystemDirectory");
            builder.AppendLine(data.VariableName, $"        Variable name");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, CheckConfiguration data, int i)
        {
            builder.AppendLine($"      Entry {i}: CheckConfiguration");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.Message, $"        Message");
            builder.AppendLine(data.Title, $"        Title");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, SearchForFile data, int i)
        {
            builder.AppendLine($"      Entry {i}: SearchForFile");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.Variable, $"        Variable");
            builder.AppendLine(data.FileName, $"        File name");
            builder.AppendLine(data.DefaultValue, $"        Default value");
            builder.AppendLine(data.MessageText, $"        Message text");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ReadWriteBinaryFile data, int i)
        {
            builder.AppendLine($"      Entry {i}: ReadWriteBinaryFile");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.FilePathname, $"        File pathname");
            builder.AppendLine(data.VariableName, $"        Variable name");
            builder.AppendLine(data.FileOffset, $"        File offset");
            builder.AppendLine(data.MaxLength, $"        Max length");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, SetVariable data, int i)
        {
            builder.AppendLine($"      Entry {i}: SetVariable");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.Variable, $"        Variable");
            builder.AppendLine(data.Value, $"        Value");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, GetEnvironmentVariable data, int i)
        {
            builder.AppendLine($"      Entry {i}: GetEnvironmentVariable");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.Variable, $"        Variable");
            builder.AppendLine(data.Environment, $"        Environment");
            builder.AppendLine(data.DefaultValue, $"        Default value");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, CheckIfFileDirExists data, int i)
        {
            builder.AppendLine($"      Entry {i}: CheckIfFileDirExists");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.Pathname, $"        Pathname");
            builder.AppendLine(data.Message, $"        Message");
            builder.AppendLine(data.Title, $"        Title");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, SetFileAttributes data, int i)
        {
            builder.AppendLine($"      Entry {i}: SetFileAttributes");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.FilePathname, $"        File pathname");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, SetFilesBuffers data, int i)
        {
            builder.AppendLine($"      Entry {i}: SetFilesBuffers");
            builder.AppendLine(data.MinimumFiles, $"        Minimum files");
            builder.AppendLine(data.MinimumBuffers, $"        Minimum buffers");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, FindFileInPath data, int i)
        {
            builder.AppendLine($"      Entry {i}: FindFileInPath");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.VariableName, $"        Variable name");
            builder.AppendLine(data.FileName, $"        File name");
            builder.AppendLine(data.DefaultValue, $"        Default value");
            builder.AppendLine(data.SearchDirectories, $"        Search directories");
            builder.AppendLine(data.Description, $"        Description");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, CheckDiskSpace data, int i)
        {
            builder.AppendLine($"      Entry {i}: CheckDiskSpace");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.ReserveSpace, $"        Reserve space");
            builder.AppendLine(data.StatusVariable, $"        Status variable");
            builder.AppendLine(data.ComponentVariables, $"        Component variables");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, InsertLineIntoTextFile data, int i)
        {
            builder.AppendLine($"      Entry {i}: InsertLineIntoTextFile");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.FileToEdit, $"        File to edit");
            builder.AppendLine(data.TextToInsert, $"        Text to insert");
            builder.AppendLine(data.SearchForText, $"        Search for text");
            builder.AppendLine(data.CommentText, $"        Comment text");
            builder.AppendLine(data.LineNumber, $"        Line number");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ParseString data, int i)
        {
            builder.AppendLine($"      Entry {i}: ParseString");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.Source, $"        Source");
            builder.AppendLine(data.PatternPosition, $"        Pattern position");
            builder.AppendLine(data.DestinationVariable1, $"        Destination variable 1");
            builder.AppendLine(data.DestinationVariable2, $"        Destination variable 2");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ExitInstallation data, int i)
        {
            builder.AppendLine($"      Entry {i}: ExitInstallation");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, SelfRegisterOCXsDLLs data, int i)
        {
            builder.AppendLine($"      Entry {i}: SelfRegisterOCXsDLLs");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.Description, $"        Description");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, InstallDirectXComponents data, int i)
        {
            builder.AppendLine($"      Entry {i}: InstallDirectXComponents");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.RootPath, $"        Root path");
            builder.AppendLine(data.LibraryPath, $"        Library path");
            builder.AppendLine(data.SizeOrOffsetOrFlag, $"        Size or offset");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, WizardBlockLoop data, int i)
        {
            // TODO: Fix this when the model is updated
            builder.AppendLine($"      Entry {i}: WizardBlockLoop");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.DirectionVariable, $"        Direction variable");
            builder.AppendLine(data.DisplayVariable, $"        Display variable");
            builder.AppendLine(data.XPosition, $"        X position");
            builder.AppendLine(data.YPosition, $"        Y position");
            builder.AppendLine(data.FillerColor, $"        Filler color");
            builder.AppendLine(data.Operand_6, $"        Operand 6");
            builder.AppendLine(data.Operand_7, $"        Operand 7");
            builder.AppendLine(data.Operand_8, $"        Operand 8");
            builder.AppendLine(data.DialogVariableValueCompare, $"        Dialog variable value compare");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ReadUpdateTextFile data, int i)
        {
            builder.AppendLine($"      Entry {i}: ReadUpdateTextFile");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.Variable, $"        Variable");
            builder.AppendLine(data.Pathname, $"        Pathname");
            builder.AppendLine(data.LanguageStrings, $"        Language strings");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, PostToHttpServer data, int i)
        {
            builder.AppendLine($"      Entry {i}: PostToHttpServer");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.URL, $"        URL");
            builder.AppendLine(data.PostData, $"        POST data");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, PromptForFilename data, int i)
        {
            builder.AppendLine($"      Entry {i}: PromptForFilename");
            builder.AppendLine(data.DataFlags, $"        Data flags");
            builder.AppendLine(data.DestinationVariable, $"        Destination variable");
            builder.AppendLine(data.DefaultExtension, $"        Default extension");
            builder.AppendLine(data.DialogTitle, $"        Dialog title");
            builder.AppendLine(data.FilterList, $"        Filter list");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, StartStopService data, int i)
        {
            builder.AppendLine($"      Entry {i}: StartStopService");
            builder.AppendLine(data.Operation, $"        Operation");
            builder.AppendLine(data.ServiceName, $"        Service name");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, CheckHttpConnection data, int i)
        {
            builder.AppendLine($"      Entry {i}: CheckHttpConnection");
            builder.AppendLine(data.UrlToCheck, $"        URL to check");
            builder.AppendLine(data.Win32ErrorTextVariable, $"        Win32 error text variable");
            builder.AppendLine(data.Win32ErrorNumberVariable, $"        Win32 error number variable");
            builder.AppendLine(data.Win16ErrorTextVariable, $"        Win16 error text variable");
            builder.AppendLine(data.Win16ErrorNumberVariable, $"        Win16 error number variable");
            builder.AppendLine();
        }

        private static void Print(StringBuilder builder, ExternalDllCall data, int i)
        {
            builder.AppendLine($"      Entry {i}: ExternalDllCall");
            if (data.Args == null)
                builder.AppendLine((string?)null, $"        Args");
            else
                builder.AppendLine(string.Join(", ", data.Args), $"        Args");
            builder.AppendLine();
        }

        #endregion

        #region Additional

        private static void Print(StringBuilder builder, DeflateEntry data, int indent, int index = -1)
        {
            string padding = string.Empty.PadLeft(indent, ' ');

            if (index >= 0)
                builder.AppendLine($"{padding}Deflate info {index}");
            else
                builder.AppendLine($"{padding}Deflate info");

            builder.AppendLine($"{padding}-------------------------");
            builder.AppendLine(data.DeflateStart, $"{padding}  Deflate start");
            builder.AppendLine(data.DeflateEnd, $"{padding}  Deflate end");
            builder.AppendLine(data.InflatedSize, $"{padding}  Inflated size");
            builder.AppendLine();
        }

        #endregion
    }
}
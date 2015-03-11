using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace SmallProjects
{
  // TODO: Filewatcher for DB dirs
  // TODO: autorunFile, autostart, readTxtLine

  class FolderAutorun
  {
    private const string app_title = "FolderAutorun";
    private const string company_name = "Svetomech";
    private const string database_name = app_title + ".db";
    private static string app_dir = String.Format("{1}{0}{2}{0}{3}", Path.DirectorySeparatorChar,
                                                  Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), company_name, app_title);
    private static string database_file = Path.Combine(app_dir, database_name);

    private static void autorunAllFiles(List<string> files, bool autorun)
    {
      if (0 == files.Count) return;

      string file = files[files.Count - 1];
      string file_path = Path.GetFullPath(file);

      // Triple sanity check
      if (!autorun || File.Exists(file_path))
      {
        string file_name = Path.GetFileNameWithoutExtension(file_path);
        string entry_name = String.Format("{0}_{1}", app_title, file_name);

        string reg_value = Convert.ToString(Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", false).GetValue(entry_name));
        using (var reg = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run"))
        {
          if (autorun)
            { if (reg_value != file_path) reg.SetValue(entry_name, file_path); }
          else
            { if (reg_value == file_path) reg.DeleteValue(entry_name); }
        }
      }

      files.Remove(file);
      autorunAllFiles(files, autorun);
    }

    private static void addTxtLine(FileStream fs, string value)
    {
        byte[] info = new UTF8Encoding(true).GetBytes(value + Environment.NewLine);
        fs.Write(info, 0, info.Length);
    }

    private static bool scanAutorunDir(string directory)
    {
      if (!File.Exists(database_file))
        return false;

      using (var sr = new StreamReader(database_file))
      {
        bool paths_equal = false;
        string line;
        while ((!paths_equal) && ((line = sr.ReadLine()) != null))
        {
          paths_equal = String.Equals(line, directory, StringComparison.OrdinalIgnoreCase);
        }
        return paths_equal;
      }
    }

    private static void saveAutorunDir(string directory)
    {
      // Sanity check
      if ((!Directory.Exists(directory)) || (null == directory)) return;

      using (var fs = new FileStream(database_file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
      using (var sr = new StreamReader(fs))
      {
        bool paths_equal = false;
        string line;

        // TODO: Implement readTxtLine method (in order to get rid of sr)
        while ((!paths_equal) && ((line = sr.ReadLine()) != null))
        {
          paths_equal = String.Equals(line, directory, StringComparison.OrdinalIgnoreCase);
        }

        if (!paths_equal)
          addTxtLine(fs, directory);
      }
    }

    private static void delAutorunDir(string directory)
    {
      // Sanity check
      if ((!Directory.Exists(directory)) || (null == directory) || (!File.Exists(database_file))) return;

      using (var sr = new StreamReader(database_file))
      using (var sw = new StreamWriter(database_file + ".upd", true))
      {
        string line;
        while ((line = sr.ReadLine()) != null)
        {
          bool paths_equal = String.Equals(line, directory, StringComparison.OrdinalIgnoreCase);
          if (!paths_equal)
            sw.WriteLine(line);
        }
      }
      File.Delete(database_file); File.Move(database_file + ".upd", database_file);
    }

    private static DialogResult? msgBox(string msg,
                                        MessageBoxButtons buttons)
    {
      return MessageBox.Show(msg, app_title, buttons);
    }

    private static DialogResult? askDialog(bool autoran)
    {
      return (autoran) ? msgBox("Disable autorun?", MessageBoxButtons.OKCancel)
                       : msgBox("Enable autorun?", MessageBoxButtons.OKCancel);
    }

    private static void exit()
    {
      msgBox("Please, do drag&drop some folder onto me.",
             MessageBoxButtons.OK);
      Environment.Exit(0);
    }

    [STAThread]
    public static void Main(string[] args)
    {
      if (0 == args.Length) exit();

      string folder_path = Path.GetFullPath(args[0]);

      // Sanity check
      if ((!Directory.Exists(folder_path)) || (null == folder_path)) exit();

      var marked_files = new List<string>(Directory.GetFiles(folder_path));

      // Double sanity check
      if (0 == marked_files.Count) return;

      if (!Directory.Exists(app_dir))
        Directory.CreateDirectory(app_dir);

      bool autoran = scanAutorunDir(folder_path);
      var btn_clicked = askDialog(autoran);

      switch (btn_clicked)
      {
        case DialogResult.OK:
          autorunAllFiles(marked_files, !autoran);
          if (!autoran)
            saveAutorunDir(folder_path);
          else
            delAutorunDir(folder_path);
          msgBox("Done!",
                 MessageBoxButtons.OK);
          break;

        default:
          // do something else
          break;
      }
    }
  }
}
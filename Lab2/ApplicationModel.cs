using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Lab2
{
    class ApplicationModel : INotifyPropertyChanged
    {
        private string folder;
        private string mask;
        private string excludeMask;
        private string find;
        private string replace;
        private SearchOption subdirectories;

        public ApplicationModel()
        {
            folder = "";
            mask = "";
            excludeMask = "";
            find = "";
            replace = "";
            subdirectories = SearchOption.TopDirectoryOnly;
        }

        public string Folder
        {
            get { return folder; }
            set
            {
                folder = value;
                OnPropertyChanged("Folder");
            }
        }

        public string Mask
        {
            get { return mask; }
            set
            {
                mask = value;
                OnPropertyChanged("Mask");
            }
        }

        public string ExcludeMask
        {
            get { return excludeMask; }
            set
            {
                excludeMask = value;
                OnPropertyChanged("ExcludeMask");
            }
        }

        public string Find
        {
            get { return find; }
            set
            {
                find = value;
                OnPropertyChanged("Find");
            }
        }

        public string Replace
        {
            get { return replace; }
            set
            {
                replace = value;
                OnPropertyChanged("Replace");
            }
        }

        public bool SubDirectories
        {
            get
            {
                if (subdirectories == SearchOption.AllDirectories) return true;
                else return false;
            }
            set
            {
                if (value) subdirectories = SearchOption.AllDirectories;
                else subdirectories = SearchOption.TopDirectoryOnly;

                OnPropertyChanged("SubDirectories");
            }
        }

        public bool checkMask(string fileName)
        {
            string[] exts = excludeMask.Split('|', ',', ';');
            string pattern = string.Empty;
            foreach (string ext in exts)
            {
                pattern += @"^";
                foreach (char symbol in ext)
                    switch (symbol)
                    {
                        case '.': pattern += @"\."; break;
                        case '?': pattern += @"."; break;
                        case '*': pattern += @".*"; break;
                        default: pattern += symbol; break;
                    }
                pattern += @"$|";
            }
            if (pattern.Length == 0) return false;
            pattern = pattern.Remove(pattern.Length - 1);
            Regex mask = new Regex(pattern, RegexOptions.IgnoreCase);
            return mask.IsMatch(System.IO.Path.GetFileName(fileName));
        }

        public void FileContainsText(object sender, DoWorkEventArgs e)
        {
            string text;
            BackgroundWorker worker = sender as BackgroundWorker;

            int count = 0;
            List<string> result = new List<string>();
            string[] allfiles = Directory.GetFiles(folder, (Mask.Equals("") ? "*" : Mask), (SearchOption)subdirectories);

            List<string> files = new List<string>();
            foreach (string temp in allfiles)
                if (!checkMask(temp)) files.Add(temp);

            double percent = ((double)1 / files.Count()) * 100;
            double progressPercentage = 0;

            foreach (string temp in files)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }

                string extention = Path.GetExtension(temp);
                
                    if (extention.Equals(".txt") || extention.Equals(".html") || extention.Equals(".css"))
                    {
                        text = File.ReadAllText(temp);
                        if (text.Contains(find))
                            result.Add(temp);
                    }
                    
                count++;
                progressPercentage += percent;
                worker.ReportProgress((int)Math.Round(progressPercentage), $"Searching files contains text ({count} / {files.Count()})");
            }

            e.Result = result;
            worker.DoWork -= FileContainsText;
        }

        public void ReplaceFiles(object sender, DoWorkEventArgs e)
        {
            string text;
            BackgroundWorker worker = sender as BackgroundWorker;

            FileContainsText(sender, e);

            List<string> files = e.Result as List<string>;
            List<string> result = new List<string>();

            int count = 0;
            double percent = ((double)1 / files.Count()) * 100;
            double progressPercentage = 0;

            foreach (string temp in files)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }

                string extention = Path.GetExtension(temp);
                string newText;

                    if (extention.Equals(".txt") || extention.Equals(".html") || extention.Equals(".css"))
                    {
                        text = File.ReadAllText(temp);
                        newText = text.Replace(find, replace);
                        File.WriteAllText(temp, newText);
                    }

                count++;
                progressPercentage += percent;
                worker.ReportProgress((int)Math.Round(progressPercentage), $"Replacing ({count} / {files.Count()})");
            }

            worker.DoWork -= ReplaceFiles;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}

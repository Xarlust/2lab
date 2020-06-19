using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Forms;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace Lab2
{
    class ApplicationViewModel : INotifyPropertyChanged
    {
        private FolderBrowserDialog folderBrowserDialog;
        private ApplicationModel applicationModel;

        private ObservableCollection<string> goodFiles;
        public ObservableCollection<string> GoodFiles
        {
            get { return goodFiles; }
        }

        public ApplicationModel AppModel
        {
            get { return applicationModel; }
        }

        public ProgressBar ProgBar
        {
            get { return progressBar; }
        }

        private BackgroundWorker worker;
        private ProgressBar progressBar;

        public ApplicationViewModel()
        {
            applicationModel = new ApplicationModel();
            folderBrowserDialog = new FolderBrowserDialog();
            progressBar = new ProgressBar();
            worker = new BackgroundWorker();

            goodFiles = new ObservableCollection<string>();
        }

        private RelayCommand find;
        public RelayCommand Find
        {
            get
            {
                return find ??
                    (find = new RelayCommand(obj =>
                    {
                        goodFiles.Clear();

                        worker = new BackgroundWorker();
                        worker.DoWork += applicationModel.FileContainsText;
                        worker.RunWorkerCompleted += RunWorkerCompleted;
                        worker.WorkerSupportsCancellation = true;
                        worker.WorkerReportsProgress = true;
                        worker.ProgressChanged += RunReportProgress;
                        worker.RunWorkerAsync();

                    }, obj => applicationModel.Folder.Any() && applicationModel.Find.Any() && !worker.IsBusy));
            }
        }

        private RelayCommand replace;
        public RelayCommand Replace
        {
            get
            {
                return replace ??
                    (replace = new RelayCommand(obj =>
                    {
                        goodFiles.Clear();

                        worker = new BackgroundWorker();
                        worker.DoWork += applicationModel.ReplaceFiles;
                        worker.RunWorkerCompleted += RunWorkerCompleted;
                        worker.WorkerSupportsCancellation = true;
                        worker.WorkerReportsProgress = true;
                        worker.ProgressChanged += RunReportProgress;
                        worker.RunWorkerAsync();

                    }, obj => applicationModel.Folder.Any() && applicationModel.Find.Any() && applicationModel.Replace.Any() && !worker.IsBusy));
            }
        }

        private RelayCommand selectFolder;
        public RelayCommand SelectFolder
        {
            get
            {
                return selectFolder ??
                    (selectFolder = new RelayCommand(obj =>
                    {
                        if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                            applicationModel.Folder = folderBrowserDialog.SelectedPath;
                    }));
            }
        }

        private RelayCommand cancelOperation;
        public RelayCommand CancelOperation
        {
            get
            {
                return cancelOperation ??
                    (cancelOperation = new RelayCommand(obj =>
                    {
                        worker.CancelAsync();
                    }, obj => worker != null && worker.IsBusy));
            }
        }

        public void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageBox.Show("Cancelled!");
                progressBar.CurrentTask = "Cancelled";
                progressBar.Progress = 0;
            }
            else
            {
                if (e.Error != null)
                {
                    MessageBox.Show("Something goes wrong!");
                    progressBar.CurrentTask = "Error";
                    progressBar.Progress = 0;
                }
                else
                {
                    List<string> files = e.Result as List<string>;

                    foreach (string temp in files) goodFiles.Add(temp);
                    progressBar.CurrentTask = $"Done! ({goodFiles.Count} / {goodFiles.Count})";
                }
            }
        }

        public void RunReportProgress(object sender, ProgressChangedEventArgs e)
        {
            progressBar.CurrentTask = e.UserState.ToString();
            progressBar.Progress = e.ProgressPercentage;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }

    public class ProgressBar : INotifyPropertyChanged
    {
        private int progress;
        public int Progress
        {
            get { return progress; }
            set
            {
                progress = value;
                OnPropertyChanged("Progress");
            }
        }

        private string currentTask;
        public string CurrentTask
        {
            get { return currentTask; }
            set
            {
                currentTask = value;
                OnPropertyChanged("CurrentTask");
            }
        }

        public ProgressBar()
        {
            progress = 0;
            currentTask = "";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }

    public class RelayCommand : ICommand
    {
        private Action<object> execute;
        private Func<object, bool> canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return this.canExecute == null || this.canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            this.execute(parameter);
        }
    }
}

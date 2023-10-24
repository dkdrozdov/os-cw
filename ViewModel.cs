using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace TaskManager_Priotity
{
    public static class Extensions
    {
        public static void Refresh<T>(this ObservableCollection<T> value)
        {
            CollectionViewSource.GetDefaultView(value).Refresh();
        }
    }

    public class ViewModel : INotifyPropertyChanged
    {
        public ViewModel()
        {
            TaskManager = new TaskManager();
        }
        TaskManager TaskManager { get; set; }
        public INotifyCollectionChanged Tasks => TaskManager.Tasks;
        public float TimeQuant { get => TaskManager.TimeQuant; set { TaskManager.TimeQuant = value; } }

        public string? NewEntryName { get; set; }
        public float? NewEntryTime { get; set; }
        public int? NewEntryPriority { get; set; }

        public void UpdateNext()
        {
            TaskManager.UpdateNext();
        }

        private RelayCommand? addCommand;
        public RelayCommand AddCommand
        {
            get
            {
                return addCommand ??
                    (addCommand = new RelayCommand(obj =>
                    {
                        TaskManager.Add(new TaskEntry(NewEntryName!, (float)NewEntryTime!, (int)NewEntryPriority!));
                        UpdateNext();
                    },
                    (obj) => NewEntryName != null &&
                                NewEntryTime != null &&
                                NewEntryPriority != null &&
                                NewEntryName.Length > 0 &&
                                NewEntryTime > 0));
            }
        }

        private RelayCommand? removeCommand;
        public RelayCommand RemoveCommand
        {
            get
            {
                return removeCommand ??
                    (removeCommand = new RelayCommand(obj =>
                    {
                        TaskManager.RemoveTask();
                        UpdateNext();
                    },
                    (obj) => TaskManager.TaskCount() > 0));
            }
        }
        public TaskEntry? SelectedTask { get; set; }

        private RelayCommand? decrementPriorityCommand;
        public RelayCommand DecrementPriorityCommand
        {
            get
            {
                return decrementPriorityCommand ??
                    (decrementPriorityCommand = new RelayCommand(obj =>
                    {
                        TaskManager.ChangePriority(SelectedTask!, SelectedTask!.Priority - 1);
                    },
                    (obj) => SelectedTask != null));
            }
        }
        private RelayCommand? incrementPriorityCommand;
        public RelayCommand IncrementPriorityCommand
        {
            get
            {
                return incrementPriorityCommand ??
                    (incrementPriorityCommand = new RelayCommand(obj =>
                    {
                        TaskManager.ChangePriority(SelectedTask!, SelectedTask!.Priority + 1);
                    },
                    (obj) => SelectedTask != null));
            }
        }
        public int TestEntryAmount { get; set; } = 10;
        private RelayCommand? generateRandomCommand;
        public RelayCommand GenerateRandomCommand
        {
            get
            {
                return generateRandomCommand ??
                    (generateRandomCommand = new RelayCommand(obj =>
                    {
                        TaskManager.Clear();

                        Random random = new(DateTime.Now.Millisecond);
                        for (int i = 0; i < TestEntryAmount; i++)
                        {
                            TaskManager.Add(new TaskEntry("Процесс#" + i, (int)random.NextInt64(5, 30), (int)random.NextInt64(0, 4)));
                        }
                        UpdateNext();
                    }));
            }
        }
        private RelayCommand? exitCommand;

        public RelayCommand ExitCommand
        {
            get
            {
                return exitCommand ??
                    (exitCommand = new RelayCommand(obj =>
                    {
                        ((Window)obj).Close();
                    }));
            }
        }
        private RelayCommand? iterateCommand;

        public RelayCommand IterateCommand
        {
            get
            {
                return iterateCommand ??
                    (iterateCommand = new RelayCommand(obj =>
                    {
                        TaskManager.Iterate();
                        UpdateNext();
                    }));
            }
        }
        public bool Playing { get; set; }
        public string PlayStateSymbol { get; set; } = "►";
        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        private RelayCommand? playPauseCommand;
        public RelayCommand PlayPauseCommand
        {
            get
            {
                return playPauseCommand ??
                    (playPauseCommand = new RelayCommand(obj =>
                    {
                        if (Playing)
                        {
                            Playing = false;
                            PlayStateSymbol = "►";
                            OnPropertyChanged("PlayStateSymbol");
                        }
                        else
                        {
                            Playing = true;
                            PlayStateSymbol = "⏸";
                            OnPropertyChanged("PlayStateSymbol");

                            Thread thread = new(() =>
                            {
                                while (true)
                                {
                                    for (int i = 0; i < 1000; i += 10)
                                    {
                                        Thread.Sleep(10);
                                        if (!Playing)
                                            return;
                                    }
                                    App.Current.Dispatcher.Invoke(delegate
                                    {
                                        TaskManager.Iterate();
                                    });
                                    if (TaskManager.TaskCount() == 0)
                                        PlayPauseCommand.Execute(obj);
                                }
                            });
                            thread.Start();
                        }
                    }));
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace OD_Trade_Mission_Tracker.Utils
{
    public static class Extentions
    {
        public static string GetDescription(this Enum value)
        {
            System.Reflection.FieldInfo fieldInfo = value.GetType().GetField(value.ToString());
            if (fieldInfo == null)
            {
                return value.ToString();
            }

            object[] attribArray = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attribArray.Length == 0 ? value.ToString() : ((DescriptionAttribute)attribArray[0]).Description;
        }

        public static void Sort<T>(this ObservableCollection<T> collection, Comparison<T> comparison)
        {
            List<T> sortableList = new(collection);
            sortableList.Sort(comparison);

            Application.Current.Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < sortableList.Count; i++)
                {
                    collection.Move(collection.IndexOf(sortableList[i]), i);
                }
            });
        }

        public static void AddToCollection<T>(this ObservableCollection<T> collection, T objectToAdd)
        {
            if (objectToAdd is null)
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                collection.Add(objectToAdd);
            });
        }

        public static void RemoveFromCollection<T>(this ObservableCollection<T> collection, T objectToRemove)
        {
            if (objectToRemove is null)
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                _ = collection.Remove(objectToRemove);
            });
        }

        public static void RemoveAtIndex<T>(this ObservableCollection<T> collection, int index)
        {
            if (collection.Count - 1 < index || index < 0)
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                collection.RemoveAt(index);
            });
        }

        public static void InsertAt<T>(this ObservableCollection<T> collection, T objectToInsert, int index)
        {
            if (index < 0)
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                collection.Insert(index, objectToInsert);
            });
        }

        public static void ClearCollection<T>(this ObservableCollection<T> collection)
        {
            if (!collection.Any())
            {
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                collection.Clear();
            });
        }

        public static string TryAddKeyboardAccellerator(this string input)
        {
            const string accellerator = "_";            // This is the default WPF accellerator symbol - used to be & in WinForms

            // If it already contains an accellerator, do nothing
            return input.Contains(accellerator) ? input : accellerator + input;
        }
    }
}

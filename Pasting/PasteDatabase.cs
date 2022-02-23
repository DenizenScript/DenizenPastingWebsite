using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using LiteDB;
using System.Threading;
using LiteDB.Engine;
using System.Text;
using FreneticUtilities.FreneticToolkit;
using DenizenPastingWebsite.Utilities;

namespace DenizenPastingWebsite.Pasting
{
    public static class PasteDatabase
    {
        /// <summary>Internal fields for the paste database.</summary>
        public static class Internal
        {
            public static LiteDatabase DB;

            public static ILiteCollection<Paste> PasteCollection;

            public static ILiteCollection<AuthHelper.UserDatabaseEntry> UserCollection;

            public class DataTracker
            {
                public long Value { get; set; }
            }

            public static ILiteCollection<DataTracker> DataCollection;

            public static DataTracker DataInstance;

            public static LockObject IDLocker = new();

            public static ILiteStorage<string> FileStorage;
        }

        /// <summary>Initializes the database handler.</summary>
        public static void Init()
        {
            if (!Directory.Exists("data"))
            {
                Directory.CreateDirectory("data");
            }
            Internal.DB = new LiteDatabase("data/pastes.ldb");
            Internal.PasteCollection = Internal.DB.GetCollection<Paste>("pastes");
            Internal.UserCollection = Internal.DB.GetCollection<AuthHelper.UserDatabaseEntry>("users");
            Internal.DataCollection = Internal.DB.GetCollection<Internal.DataTracker>("data");
            Internal.DataInstance = Internal.DataCollection.FindById(0);
            if (Internal.DataInstance == null)
            {
                Internal.DataInstance = new Internal.DataTracker() { Value = 0 };
                Internal.DataCollection.Insert(0, Internal.DataInstance);
            }
            Internal.FileStorage = Internal.DB.FileStorage;
            Task.Factory.StartNew(() =>
            {
                Task.Delay(TimeSpan.FromSeconds(5)).Wait();
                foreach (AuthHelper.UserDatabaseEntry user in Internal.UserCollection.FindAll())
                {
                    if (user.LastTimeVerified + AuthHelper.InvalidateDelay < AuthHelper.CurrentTimestamp())
                    {
                        Internal.UserCollection.Delete(user.UserID);
                    }
                }
            });
        }

        public static void Shutdown()
        {
            if (Internal.DB != null)
            {
                Internal.DB.Dispose();
                Internal.DB = null;
            }
        }

        /// <summary>Gets the next paste ID, automatically incrementing the ID in the process.</summary>
        public static long GetNextPasteID()
        {
            lock (Internal.IDLocker)
            {
                long result = Internal.DataInstance.Value++;
                Internal.DataCollection.Upsert(0, Internal.DataInstance);
                return result;
            }
        }

        /// <summary>Submits a new paste, adding it to the database.</summary>
        /// <param name="paste">The paste to insert.</param>
        public static void SubmitPaste(Paste paste)
        {
            if (paste.Raw.Length + paste.Formatted.Length > 7 * 1024 * 1024)
            {
                if (paste.IsInFileStore)
                {
                    Internal.FileStorage.Delete($"/paste/raw/{paste.ID}.txt");
                    Internal.FileStorage.Delete($"/paste/formatted/{paste.ID}.txt");
                }
                paste.IsInFileStore = true;
                Internal.FileStorage.Upload($"/paste/raw/{paste.ID}.txt", $"{paste.ID}.txt", new MemoryStream(Encoding.UTF8.GetBytes(paste.Raw)));
                Internal.FileStorage.Upload($"/paste/formatted/{paste.ID}.txt", $"{paste.ID}.txt", new MemoryStream(Encoding.UTF8.GetBytes(paste.Formatted)));
                paste.Raw = null;
                paste.Formatted = null;
            }
            else
            {
                paste.IsInFileStore = false;
            }
            Internal.PasteCollection.Upsert(paste.ID, paste);
        }

        /// <summary>Fills content of a paste object from file store if necessary.</summary>
        public static void FillPaste(Paste paste)
        {
            if (paste != null && paste.IsInFileStore)
            {
                MemoryStream stream = new();
                Internal.FileStorage.Download($"/paste/raw/{paste.ID}.txt", stream);
                paste.Raw = Encoding.UTF8.GetString(stream.ToArray());
                stream = new MemoryStream();
                Internal.FileStorage.Download($"/paste/formatted/{paste.ID}.txt", stream);
                paste.Formatted = Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        /// <summary>Tries to get a paste.</summary>
        public static bool TryGetPaste(long id, out Paste paste)
        {
            paste = Internal.PasteCollection.FindById(id);
            FillPaste(paste);
            return paste != null;
        }
    }
}

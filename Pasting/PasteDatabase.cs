using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
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

            public static ILiteCollection<PasteUser> TrackedSenders;

            public class DataTracker
            {
                public long Value { get; set; }
            }

            public static ILiteCollection<DataTracker> DataCollection;

            public static DataTracker DataInstance;

            public static LockObject IDLocker = new();

            public static ILiteStorage<string> FileStorage;

            public static LockObject PasteLock = new();
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
            Internal.TrackedSenders = Internal.DB.GetCollection<PasteUser>("tracked_senders");
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
            if (Internal.DB is not null)
            {
                lock (Internal.PasteLock)
                {
                    Internal.DB.Dispose();
                }
                Internal.DB = null;
            }
        }

        /// <summary>Gets the next paste ID number without incrementing.</summary>
        public static long GetTotalPasteCount()
        {
            lock (Internal.IDLocker)
            {
                return Internal.DataInstance.Value;
            }
        }

        /// <summary>Gets the next paste ID, automatically incrementing the ID in the process.</summary>
        public static long GetNextPasteID()
        {
            lock (Internal.IDLocker)
            {
                long result = Internal.DataInstance.Value++;
                lock (Internal.PasteLock)
                {
                    Internal.DataCollection.Upsert(0, Internal.DataInstance);
                }
                return result;
            }
        }

        /// <summary>Submits a new paste, translating raw data to binary compressed data, and adding it to the database.</summary>
        /// <param name="paste">The paste to insert.</param>
        public static void SubmitPaste(Paste paste)
        {
            lock (Internal.PasteLock)
            {
                if (paste.IsInFileStore)
                {
                    Internal.FileStorage.Delete($"/paste/raw/{paste.ID}.txt");
                    Internal.FileStorage.Delete($"/paste/formatted/{paste.ID}.txt");
                    paste.IsInFileStore = false;
                }
                paste.StoredRaw = GZip(Encoding.UTF8.GetBytes(paste.Raw));
                paste.StoredFormatted = GZip(Encoding.UTF8.GetBytes(paste.Formatted));
                paste.Raw = null;
                paste.Formatted = null;
                paste.IsCompressed = true;
                if (paste.StoredRaw.Length + paste.StoredFormatted.Length > 7 * 1024 * 1024)
                {
                    paste.IsInFileStore = true;
                    Internal.FileStorage.Upload($"/paste/raw/{paste.ID}.txt", $"{paste.ID}.txt", new MemoryStream(paste.StoredRaw));
                    Internal.FileStorage.Upload($"/paste/formatted/{paste.ID}.txt", $"{paste.ID}.txt", new MemoryStream(paste.StoredFormatted));
                }
                Internal.PasteCollection.Upsert(paste.ID, paste);
            }
        }

        /// <summary>Fills text content of a paste object from compressed data.</summary>
        public static void FillPasteStrings(Paste paste, bool includeFormatted = true)
        {
            if (paste is null)
            {
                return;
            }
            if (paste.IsCompressed)
            {
                if (paste.Raw is null && paste.StoredRaw is not null)
                {
                    paste.Raw = Encoding.UTF8.GetString(UnGZip(paste.StoredRaw));
                }
                if (paste.Formatted is null && paste.StoredFormatted is not null && includeFormatted)
                {
                    paste.Formatted = Encoding.UTF8.GetString(UnGZip(paste.StoredFormatted));
                }
            }
            else
            {
                if (paste.Raw is null && paste.StoredRaw is not null)
                {
                    paste.Raw = Encoding.UTF8.GetString(paste.StoredRaw);
                }
                if (paste.Formatted is null && paste.StoredFormatted is not null && includeFormatted)
                {
                    paste.Formatted = Encoding.UTF8.GetString(paste.StoredFormatted);
                }
            }
            paste.StoredRaw = null;
            paste.StoredFormatted = null;
        }

        /// <summary>Fills text content of a paste object from file store.</summary>
        public static void FillPasteFromStorage(Paste paste, bool includeFormatted = true)
        {
            if (paste is null)
            {
                return;
            }
            if (paste.IsInFileStore)
            {
                lock (Internal.PasteLock)
                {
                    MemoryStream stream = new();
                    Internal.FileStorage.Download($"/paste/raw/{paste.ID}.txt", stream);
                    paste.StoredRaw = stream.ToArray();
                    if (includeFormatted)
                    {
                        stream = new MemoryStream();
                        Internal.FileStorage.Download($"/paste/formatted/{paste.ID}.txt", stream);
                        paste.StoredFormatted = stream.ToArray();
                    }
                }
            }
        }

        /// <summary>Fills text content of a paste object from file store or compressed data.</summary>
        public static void FillPaste(Paste paste, bool includeFormatted = true)
        {
            if (paste is null)
            {
                return;
            }
            if (paste.IsInFileStore)
            {
                FillPasteFromStorage(paste, includeFormatted);
            }
            FillPasteStrings(paste, includeFormatted);
        }

        /// <summary>Tries to get a paste.</summary>
        public static bool TryGetPaste(long id, out Paste paste)
        {
            lock (Internal.PasteLock)
            {
                paste = Internal.PasteCollection.FindById(id);
                FillPaste(paste);
            }
            return paste != null;
        }

        /// <summary>Gets the user data for a specified sender address.</summary>
        public static PasteUser GetUser(string origin)
        {
            origin = origin.Replace(", response=microv2", "").Replace("X-Forwarded-For: ", "");
            origin = PasteServer.AllowedSenderText.TrimToMatches(origin);
            PasteUser user;
            lock (Internal.PasteLock)
            {
                user = Internal.TrackedSenders.FindById(origin);
            }
            if (user is not null)
            {
                return user;
            }
            return new PasteUser() { SenderID = origin, CurrentStatus = PasteUser.Status.NORMAL };
        }

        /// <summary>Adds or updates user data to the database.</summary>
        public static void ResubmitUser(PasteUser user)
        {
            lock (Internal.PasteLock)
            {
                Internal.TrackedSenders.Upsert(user);
            }
        }

        /// <summary>Compresses a byte array using the GZip algorithm.</summary>
        /// <param name="input">Non-compressed data.</param>
        /// <returns>Compressed data.</returns>
        public static byte[] GZip(byte[] input)
        {
            using MemoryStream memstream = new();
            using GZipStream GZStream = new(memstream, CompressionMode.Compress);
            GZStream.Write(input, 0, input.Length);
            GZStream.Flush();
            return memstream.ToArray();
        }

        /// <summary>Decompress a byte array using the GZip algorithm.</summary>
        /// <param name="input">Compressed data.</param>
        /// <returns>Non-compressed data.</returns>
        public static byte[] UnGZip(byte[] input)
        {
            using MemoryStream output = new();
            using MemoryStream memstream = new(input);
            using GZipStream GZStream = new(memstream, CompressionMode.Decompress);
            GZStream.CopyTo(output);
            GZStream.Flush();
            return output.ToArray();
        }
    }
}

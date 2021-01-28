using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using LiteDB;
using System.Threading;

namespace DenizenPastingWebsite
{
    public static class PasteDatabase
    {
        /// <summary>
        /// Internal fields for the paste database.
        /// </summary>
        public static class Internal
        {
            public static LiteDatabase DB;

            public static ILiteCollection<Paste> PasteCollection;

            public class DataTracker
            {
                public long Value { get; set; }
            }

            public static ILiteCollection<DataTracker> DataCollection;

            public static DataTracker DataInstance;

            public static Object IDLocker = new Object();
        }

        /// <summary>
        /// Maximum raw length (in characters) of a paste.
        /// Defaults to 5 MiB.
        /// </summary>
        public static int MaxPasteRawLength = 5 * 1024 * 1024;

        /// <summary>
        /// Initializes the database handler.
        /// </summary>
        public static void Init()
        {
            if (!Directory.Exists("data"))
            {
                Directory.CreateDirectory("data");
            }
            Internal.DB = new LiteDatabase("data/pastes.ldb");
            Internal.PasteCollection = Internal.DB.GetCollection<Paste>("pastes");
            Internal.DataCollection = Internal.DB.GetCollection<Internal.DataTracker>("data");
            Internal.DataInstance = Internal.DataCollection.FindById(0);
            if (Internal.DataInstance == null)
            {
                Internal.DataInstance = new Internal.DataTracker() { Value = 0 };
                Internal.DataCollection.Insert(0, Internal.DataInstance);
            }
        }

        /// <summary>
        /// Gets the next paste ID, automatically incrementing the ID in the process.
        /// </summary>
        public static long GetNextPasteID()
        {
            lock (Internal.IDLocker)
            {
                long result = Internal.DataInstance.Value++;
                Internal.DataCollection.Upsert(0, Internal.DataInstance);
                return result;
            }
        }

        /// <summary>
        /// Submits a new paste, assigning it a new ID and added it to the database.
        /// </summary>
        /// <param name="paste">The paste to insert.</param>
        public static void SubmitPaste(Paste paste)
        {
            paste.ID = GetNextPasteID();
            Internal.PasteCollection.Insert(paste.ID, paste);
        }
    }
}

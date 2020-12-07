﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenBlam.Core.Streams;
using System;
using System.IO;

namespace OpenBlam.Core.Tests.Streams
{
    [TestClass]
    public class ReadOnlyFileStreamTests
    {
        private const int values = ReadOnlyFileStream.BufferSize * 4;
        string testFilePath;

        [TestInitialize]
        public void Init()
        {
            testFilePath = Path.GetTempFileName();
            using var fs = new FileStream(testFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var writer = new BinaryWriter(fs);

            // Large enough to make buffer reload a few times
            for(var i = 0; i < values; i++)
            {
                writer.Write((uint)i);
            }
        }

        [TestMethod]
        public void UintOverload_ValuesMatch()
        {
            using var fs = new FileStream(testFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(fs);
            using var rofs = new ReadOnlyFileStream(testFilePath);

            for (var i = 0; i < values; i++)
            {
                Assert.AreEqual(reader.ReadUInt32(), rofs.ReadUInt32At(i * sizeof(uint)));
            }
        }

        [TestMethod]
        public void UintReader_ValuesMatch()
        {
            using var fs = new FileStream(testFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(fs);
            using var rofs = new ReadOnlyFileStream(testFilePath);
            using var roreader = new BinaryReader(rofs);

            for (var i = 0; i < values; i++)
            {
                Assert.AreEqual(reader.ReadUInt32(), roreader.ReadUInt32());
            }
        }


        [TestMethod]
        public void UintReader_ValuesMatchReverse()
        {
            using var fs = new FileStream(testFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(fs);
            using var rofs = new ReadOnlyFileStream(testFilePath);
            using var roreader = new BinaryReader(rofs);

            for (var i = 0; i < values; i++)
            {
                fs.Position = fs.Length - (i * sizeof(uint)) - sizeof(uint);
                rofs.Position = fs.Length - (i * sizeof(uint)) - sizeof(uint);

                Assert.AreEqual(reader.ReadUInt32(), roreader.ReadUInt32());
            }
        }

        [TestMethod]
        public void UintReader_ValuesMatchRandom()
        {
            using var fs = new FileStream(testFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(fs);
            using var rofs = new ReadOnlyFileStream(testFilePath);
            using var roreader = new BinaryReader(rofs);

            var rand = new Random(42);

            for (var i = 0; i < values; i++)
            {
                var pos = rand.Next(0, values) * sizeof(uint);

                fs.Position = pos;
                rofs.Position = pos;

                Assert.AreEqual(reader.ReadUInt32(), roreader.ReadUInt32(), $"Failed on {i}");
            }
        }

        [TestCleanup]
        public void Clean()
        {
            File.Delete(testFilePath);
        }
    }
}

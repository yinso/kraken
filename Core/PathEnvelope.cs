using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using Kraken.Util;

namespace Kraken.Core
{
    /// <summary>
    /// Path.
    /// 
    /// Path represents a virtual file path that points to a particular blob to be retrieved.
    /// 
    /// Path is *versioned* - i.e. each change of the file will result in this file being stored modified.
    /// 
    /// By default, path is only about the blobs, i.e. not about the directory itself. So directory can change
    /// without being snapshotted.
    /// 
    /// This is to support *temporary* workspace that is *also* managed. Most of the time, this is how people
    /// work (i.e. not thinking explicitly about managing the directory structures, and each files don't really
    /// have strong relationship with each other if we are using this to manage ALL of the files within a space).
    /// 
    /// Now the key is this - Path *is* independent from the Tree, but it represents a particular starting point.
    /// 
    /// We can also define an arbitrary starting point as a tree to manage. But that's a separate conversation.
    /// 
    /// *******
    /// Path format.
    /// *******
    /// 
    /// By default, Path is really a filesystem file that states a particular position.
    /// 
    /// Path itself holds metadata of itself...
    /// 
    /// Since we'll be reading more than writing, this file will be rewritten rather than *appended* to (so we can
    /// read the latest version from the top, rather than from the bottom).
    /// 
    /// Kraken doesn't actually require using Path, but Path is much nicer than remembering the checksum for manipulation
    /// when the usage is human-based (it's not too bad for software either - they'll just store the original path
    /// (plus the account path) rather than remembering the checksum
    /// 
    /// Within the code it looks like the following.
    /// 
    /// PathStore.SavePath(filePath, toPath); // toPath often looks like the filePath in someway, but likely have different root.
    /// 
    /// Let's think through what the format would be.
    /// 
    /// 1 - it ought to allow for metadata extension. This would be doable in the key/value pair scenario with MIME-like headers.
    /// 2 - we are not going to store access time... that information is practically useless.
    /// 3 - timestamp can be attached to the modification of each of the time.
    /// 4 - it needs to be easily parseable (meaning that we already have the tool to do it or it can be easily written).
    /// 5 - do we store permission at this level? generally speaking... no, especially if it's meant to be used across systems.
    ///     but we ought to allow for the possibility to extend for it.
    /// 
    /// mandatory fields.
    /// 
    /// timestamp
    /// checksum (for mapping to the blobstore).
    /// 
    /// if we keep the file name - there is no reason we have to deal with mimetype in the system.
    /// 
    /// this is the nice part.
    /// 
    /// pretty much anything else is a extended attribute...
    /// 
    /// if we want fast reading - it is still fastest to just read a line.
    /// and then parse by space.
    /// 
    /// attributes should be URI encoded... ;)
    /// 
    /// it would be fun to just make it a querystring! ;) that'll be cool.
    /// 
    /// keep in mind that the attributes will always show up on the top, but the timestamp/checksum are actually a running list.
    /// 
    /// I think the a query string will be the way to go.
    /// 
    /// Another way is to have a .version of the file - and we'll just maintain the latest in the current version (that makes it quite cheap to write).
    /// 
    /// and the .version file will have all the versions **appended***... truthfully - very few files should have been updated that many times.
    /// 
    /// (even a thousand lines would be trivial to read).
    /// 
    /// And do we version the metadata? generally not for sure (i.e. makes no sense to track the metadata in the past).
    /// 
    /// So how do we have the versions of the file stored? we want to optimize for the latest read, 
    /// and the version is just *gravy* somewhere...
    /// 
    /// we can revive our UUID approach - i.e. each file would have an UUID. and the UUID
    /// will then have the versions...
    /// 
    /// i.e. we'll hold the UUID somewhere to show us the versions of the files... NOTE this differs from the folders again.
    /// (but might be useful).
    /// 
    /// UUID will be 32-bit GUID... so while it would be similar to 
    /// 
    /// Let's continue to think through this problem.
    /// 
    /// In practice, even reading a few thousand lines is not much of a problem. Obviously if we can make this a
    /// 
    /// we should also specify on how to read/write out a Path file.
    /// 
    /// It's again important to figure out what exactly is a PathFile...
    /// 
    /// Note - a Path is a collection with a prefix envelope? (assuming that we want to track some additional info
    /// to identify that this is a Path file.
    /// 
    /// strictly speaking that's not too hard.
    /// 
    /// The first line can just be
    /// 
    /// Path <version>
    /// (by the way we don't really care about symlink do we?) Maybe we do... but pretty much everything in this system
    /// is a symlink... hmm...
    /// 
    /// anyways - we'll write this backwards, so it'll mostly be fast (for reading - not necessarily for writing if there are 
    /// hundreds of thousands of version being built-up... that should be almost difficult to fathom for a single file).
    /// 
    /// 
    /// </summary>

    public class PathEnvelope {
        ResourceType type = ResourceType.Path;
        public short Version { get; internal set; }
        public string Checksum { get; internal set; }
        public DateTime Created { get; internal set; } 
        public DateTime LastModified { get; internal set; }
        public long Length { get; internal set; } // optimize additional information.
        public NameValueCollection KeyVals { get; internal set; }

        public PathEnvelope() { 
            KeyVals = new NameValueCollection();
        } 

        public static PathEnvelope Parse(Reader reader)
        {
            string line = reader.ReadLine();
            string[] parts = line.Split(new char[]{' '});
            if (parts.Length < 6) 
                throw new Exception(string.Format("invalid_path_envelope_format: {0}", line));
            try
            {
                ResourceType type = (ResourceType)Enum.Parse(typeof(ResourceType), parts [0]);
                PathEnvelope envelope = new PathEnvelope();
                envelope.Version = short.Parse(parts[1]);
                envelope.Checksum = parts[2]; // this is the checksum.
                envelope.Created = DateTime.Parse(parts[3]).ToUniversalTime();
                envelope.LastModified = DateTime.Parse(parts[4]).ToUniversalTime();
                envelope.Length = long.Parse(parts[5]);
                if (parts.Length > 6) {
                    // we'll parse the collection.
                    envelope.KeyVals = UriUtil.ParseQueryString(parts[4]);
                }
                return envelope;
            } catch (Exception e)
            {
                throw new Exception(string.Format("invalid_path_envelop: {0}", line), e);
            }
        }

        public byte[] Serialize()
        {
            string line;
            if (KeyVals.Count > 0)
            {
                line = string.Format("{0} {1} {2} {3} {4} {5} {6}\r\n"
                                     , type
                                     , Version
                                     , Checksum
                                     , Created.ToString("o")
                                     , LastModified.ToString("o")
                                     , Length.ToString()
                                     , UriUtil.NameValueCollectionToQueryString(KeyVals));
            } else
            {
                line = string.Format("{0} {1} {2} {3} {4} {5}\r\n"
                                     , type
                                     , Version
                                     , Checksum
                                     , Created.ToString("o")
                                     , LastModified.ToString("o")
                                     , Length.ToString()
                                     );
            }
            return Encoding.UTF8.GetBytes(line);
        }

        public void WriteTo(Stream s)
        {
            byte[] bytes = Serialize();
            s.Write(bytes, 0, bytes.Length);
        }

    }
    
}

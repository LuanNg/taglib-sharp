﻿//
// Tag.cs:
//
// Author:
//   Julien Moutte <julien@fluendo.com>
//   Sebastien Mouy <starwer@laposte.net>
//
// Copyright (C) 2011 FLUENDO S.A.
//
// This library is free software; you can redistribute it and/or modify
// it  under the terms of the GNU Lesser General Public License version
// 2.1 as published by the Free Software Foundation.
//
// This library is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307
// USA
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TagLib.Matroska
{
    /// <summary>
    /// Describes a Matroska Tag.
    /// A <see cref="Tag"/> object may contain several <see cref="SimpleTag"/>.
    /// </summary>
    public class Tag : TagLib.Tag
    {
        #region Private fields/Properties

        /// <summary>
        /// Define if this represent a video content (true), or an audio content (false)
        /// </summary>
        private bool IsVideo
        {
            get { return Tags == null || Tags.IsVideo; }
        }


        #endregion


        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tags">the Tags this Tag should be added to.</param>
        /// <param name="targetTypeValue">the Target Type ValueTags this Tag represents.</param>
        public Tag(Tags tags = null, ushort targetTypeValue = 0)
        {
            if (targetTypeValue != 0) TargetTypeValue = targetTypeValue;
            Tags = tags;
            if(tags != null) tags.Add(this);
        }


        #endregion


        #region Methods

        /// <summary>
        /// Return a Tag of a certain Target type.  
        /// </summary>
        /// <param name="create">Create one if it doesn't exist yet.</param>
        /// <param name="targetTypeValue">Target Type Value of the .</param>
        /// <returns>the Tag representing the collection</returns>
        private Tag TagsGet(bool create, ushort targetTypeValue)
        {
            Tag ret = Tags?.Get(targetTypeValue, true);
            if (ret == null && create)
            {
                ret = new Tag(Tags, targetTypeValue);
            }
            return ret;
        }


        /// <summary>
        /// Return the Tag representing the Album the medium belongs to.  
        /// </summary>
        /// <param name="create">Create one if it doesn't exist yet.</param>
        /// <returns>the Tag representing the collection</returns>
        private Tag TagsAlbum(bool create)
        {
            Tag ret = null;
            if (Tags != null)
            {
                ret = Tags.Album;
                if (ret == null && create)
                {
                    var targetTypeValue = Tags.IsVideo ? (ushort)70 : (ushort)50;
                    ret = new Tag(Tags, targetTypeValue);
                }
            }
            return ret;
        }


        /// <summary>
        /// Remove a Tag
        /// </summary>
        /// <param name="key">Tag Name</param>
        /// <param name="subkey">Nested SimpleTag to find (if non null) Tag name</param>
        public void Remove(string key, string subkey = null)
        {
            List<SimpleTag> list = null;
            if (SimpleTags.TryGetValue(key, out list))
            {
                if (list != null)
                {
                    if (subkey != null)
                    {
                        foreach (var stag in list)
                        {
                            if (stag.SimpleTags != null)
                            {
                                List<SimpleTag> slist = null;
                                stag.SimpleTags.TryGetValue(subkey, out slist);
                                if (slist != null)
                                {
                                    if (list.Count > 1)
                                    {
                                        if(slist.Count>0) slist.RemoveAt(0);
                                    }
                                    else
                                    {
                                        slist.Clear();
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        list.Clear();
                    }
                }

                if (subkey == null) SimpleTags.Remove(key);
            }
        }


        /// <summary>
        /// Set a Tag value. A null value removes the Tag.
        /// </summary>
        /// <param name="key">Tag Name</param>
        /// <param name="subkey">Nested SimpleTag to find (if non null) Tag name</param>
        /// <param name="value">value to be set. A list can be passed for a subtag by separating the values by ';'</param>
        public void Set(string key, string subkey, string value)
        {
            if (value == null)
            {
                Remove(key, subkey);
                return;
            }

            List<SimpleTag> list = null;

            SimpleTags.TryGetValue(key, out list);

            if (list == null)
                SimpleTags[key] = list = new List<SimpleTag>(1); 

            if (list.Count == 0)
                list.Add(new SimpleTag());

            if (subkey == null)
            {
                list[0].Value = value;
            }
            else
            {
                if (list[0].SimpleTags == null)
                    list[0].SimpleTags = new Dictionary<string, List<SimpleTag>>(StringComparer.OrdinalIgnoreCase);

                List<SimpleTag> slist = null;
                list[0].SimpleTags.TryGetValue(subkey, out slist);

                if (slist == null)
                    slist = new List<SimpleTag>(1);

                list[0].SimpleTags[subkey] = slist;

                if (slist.Count == 0)
                    slist.Add(new SimpleTag());

                // Sub-values
                var svalues = value.Split(';');
                int j;
                for (j = 0; j < svalues.Length; j++)
                {
                    SimpleTag subtag;
                    if (j >= slist.Count)
                        slist.Add(subtag = new SimpleTag());
                    else
                        subtag = slist[j];

                    subtag.Value = svalues[j];
                }

                if (j < slist.Count)
                    slist.RemoveRange(j, slist.Count - j);
            }

        }

        /// <summary>
        /// Set a Tag value as unsigned integer. Please note that a value zero removes the Tag.
        /// </summary>
        /// <param name="key">Tag Name</param>
        /// <param name="subkey">Nested SimpleTag to find (if non null) Tag name</param>
        /// <param name="value">unsigned integer value to be set</param>
        /// <param name="format">Format for string convertion to be used (default: null)</param>
        public void Set(string key, string subkey, uint value, string format = null)
        {
            if (value == 0)
            {
                Remove(key, subkey);
                return;
            }

            Set(key, subkey, value.ToString(format, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Create or overwrite the actual tags of a given name/sub-name by new values. 
        /// </summary>
        /// <param name="key">Tag Name</param>
        /// <param name="subkey">Nested SimpleTag to find (if non null) Tag name</param>
        /// <param name="values">Array of values. for each subtag value, a list can be passed by separating the values by ';'</param>
        public void Set(string key, string subkey, string[] values)
        {
            if (values == null)
            {
                Remove(key, subkey);
                return;
            }

            List<SimpleTag> list = null;

            SimpleTags.TryGetValue(key, out list);

            if (list == null)
                SimpleTags[key] = list = new List<SimpleTag>(1);

            int i;
            for (i = 0; i < values.Length; i++)
            {
                SimpleTag stag;
                if (i >= list.Count)
                    list.Add(stag = new SimpleTag());
                else
                    stag = list[i];

                if (subkey == null)
                {
                    stag.Value = values[i];
                }
                else
                {
                    if (stag.SimpleTags == null)
                        stag.SimpleTags = new Dictionary<string, List<SimpleTag>>(StringComparer.OrdinalIgnoreCase);

                    List<SimpleTag> slist = null;
                    stag.SimpleTags.TryGetValue(subkey, out slist);

                    if (slist == null)
                        slist = new List<SimpleTag>(1);

                    stag.SimpleTags[subkey] = slist;

                    // Sub-values
                    var svalues = values[i].Split(';');
                    int j;
                    for (j = 0; j < svalues.Length; j++)
                    {
                        SimpleTag subtag;
                        if (j >= slist.Count)
                            slist.Add(subtag = new SimpleTag());
                        else
                            subtag = slist[j];

                        subtag.Value = svalues[j];
                    }

                    if (j < slist.Count)
                        slist.RemoveRange(j, slist.Count - j);
                }
            }


            if(subkey == null && i < list.Count)
                list.RemoveRange(i, list.Count - i);
        }

        /// <summary>
        /// Retrieve a list of SimpleTag sahring the same TagName (key).
        /// </summary>
        /// <param name="key">Tag name</param>
        /// <param name="subkey">Nested SimpleTag to find (if non null) Tag name</param>
        /// <param name="recu">Also search in parent Tag if true (default: true)</param>
        /// <returns>Array of values</returns>
        public List<SimpleTag> GetSimpleTags(string key, string subkey = null, bool recu = true)
        {
            List<SimpleTag> mtags;

            if ((!SimpleTags.TryGetValue(key, out mtags) || mtags == null) && recu)
            {
                Tag tag = this;
                while ((tag = tag.Parent) != null && !tag.SimpleTags.TryGetValue(key, out mtags)) ;
            }

            // Handle Nested SimpleTags
            if (subkey != null && mtags != null)
            {
                var subtags = new List<SimpleTag>(mtags.Count);

                foreach (var stag in mtags)
                {
                    if (stag.SimpleTags != null)
                    {
                        List<SimpleTag> list = null;
                        stag.SimpleTags.TryGetValue(subkey, out list);
                        if(mtags.Count>1)
                        {
                            subtags.Add(list?[0]);
                        }
                        else
                        {
                            subtags = list;
                        }
                    }
                }

                return subtags;
            }

            return mtags;
        }

        /// <summary>
        /// Retrieve a Tag list
        /// </summary>
        /// <param name="key">Tag name</param>
        /// <param name="subkey">Nested SimpleTag to find (if non null) Tag name</param>
        /// <param name="recu">Also search in parent Tag if true (default: true)</param>
        /// <returns>Array of values</returns>
        public string[] Get(string key, string subkey = null, bool recu = true)
        {
            string[] ret = null;

            List<SimpleTag> list = GetSimpleTags(key, subkey, recu);

            if (list != null)
            {
                ret = new string[list.Count];
                for (int i=0; i < list.Count; i++)
                {
                    ret[i] = list[i];
                }
            }

            return ret;
        }

        /// <summary>
        /// Retrieve a Tag value as string
        /// </summary>
        /// <param name="key">Tag name</param>
        /// <param name="subkey">Nested SimpleTag to find (if non null) Tag name</param>
        /// <param name="recu">Also search in parent Tag if true (default: true)</param>
        /// <returns>Tag value</returns>
        private string GetString(string key, string subkey = null, bool recu = true)
        {
            string ret = null;

            List<SimpleTag> list = GetSimpleTags(key, subkey, recu);
            if (list != null && list.Count>0) ret = list[0];

            return ret;
        }


        /// <summary>
        /// Retrieve a Tag value as unsigned integer
        /// </summary>
        /// <param name="key">Tag name</param>
        /// <param name="subkey">Nested SimpleTag to find (if non null) Tag name</param>
        /// <param name="recu">Also search in parent Tag if true (default: false)</param>
        /// <returns>Tag value as unsigned integer</returns>
        private uint GetUint(string key, string subkey = null, bool recu = false)
        {
            uint ret = 0;
            string val = GetString(key, subkey, recu);

            if (val != null)
            {
                uint.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out ret);
            }

            return ret;
        }


        #endregion


        #region Properties

        /// <summary>
        /// Retrieve a list of Matroska Tags 
        /// </summary>
        public Tags Tags { private set; get; }


        /// <summary>
        /// Retrieve the parent Tag, of higher TargetTypeValue (if any, null if none)
        /// </summary>
        public Tag Parent
        {
            get
            {
                Tag ret = null;
                if (Tags != null)
                {
                    int i = Tags.IndexOf(this);
                    if (i > 0) ret = Tags[i - 1];
                }
                return ret;
            }
        }

        /// <summary>
        ///    Gets the Matroska Target Type Value of this Tag.
        /// </summary>
        public ushort TargetTypeValue
        {
            get
            {
                return _TargetTypeValue;
            }
            set
            {
                // Coerce: Valid values are: 10 20 30 40 50 60 70
                _TargetTypeValue = (ushort)
                    ( value > 70 ? 70
                    : value < 10 ? 10
                    : ((value + 5) / 10) * 10
                    );

                // Make sure the List keeps ordered
                if (Tags != null)
                {
                    if(TargetType == null)  Tags.MakeTargetType(_TargetTypeValue);
                    Tags.Add(this);
                }
            }
        }
        private ushort _TargetTypeValue = 0;

        /// <summary>
        ///    Gets the Matroska Target Type (informational name) of this Tag.
        /// </summary>
        public string TargetType = null;

        /// <summary>
        /// Array of unique IDs to identify the Track(s) the tags belong to. If the value is 0 at this level, the tags apply to all tracks in the Segment.
        /// </summary>
        public ulong[] TrackUID = null;

        /// <summary>
        /// Array ofunique IDs to identify the EditionEntry(s) the tags belong to. If the value is 0 at this level, the tags apply to all editions in the Segment.
        /// </summary>
        public ulong[] EditionUID = null;

        /// <summary>
        ///  Array ofunique IDs to identify the Chapter(s) the tags belong to. If the value is 0 at this level, the tags apply to all chapters in the Segment. 
        /// </summary>
        public ulong[] ChapterUID = null;

        /// <summary>
        /// Array of unique IDs to identify the Attachment(s) the tags belong to. If the value is 0 at this level, the tags apply to all the attachments in the Segment.
        /// </summary>
        public ulong[] AttachmentUID = null;


        /// <summary>
        /// List SimpleTag contained in the current Tag (must never be null)
        /// </summary>
        public Dictionary<string, List<SimpleTag>> SimpleTags = new Dictionary<string, List<SimpleTag>>(StringComparer.OrdinalIgnoreCase);



        /// <summary>
        ///    Gets the tag types contained in the current instance.
        /// </summary>
        /// <value>
        ///    Always <see cref="TagTypes.Matroska" />.
        /// </value>
        public override TagTypes TagTypes
        {
            get { return TagTypes.Matroska; }
        }

        /// <summary>
        ///    Gets and sets the title for the media described by the
        ///    current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> object containing the title for
        ///    the media described by the current instance or <see
        ///    langword="null" /> if no value is present.
        /// </value>
        /// <remarks>
        ///    This property is implemented using the TITLE tag and the Segment Title.
        /// </remarks>
        public override string Title
        {
            get
            {
                var ret = GetString("TITLE");
                if (ret == null && Tags?.Medium == this) ret = Tags.Title;
                return ret;
            }
            set
            {
                Set("TITLE", null, value);
                if (Tags?.Medium == this) Tags.Title = value;
            }
        }

        /// <summary>
        ///    Gets and sets the sort names for the Track Title of
        ///    the media described by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> containing the sort name of 
        ///    the Track Title of the media described by the current
        ///    instance or null if no value is present.
        /// </value>
        /// <remarks>
        ///    This property is implemented using the nested Matroska 
        ///    SimpleTag "SORT_WITH" inside the "TITLE" SimpleTag.
        /// </remarks>
        public override string TitleSort
        {
            get { return GetString("TITLE", "SORT_WITH"); }
            set { Set("TITLE", "SORT_WITH", value); }
        }

        /// <summary>
        ///    Gets and sets the performers or artists who performed in
        ///    the media described by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string[]" /> containing the performers or
        ///    artists who performed in the media described by the
        ///    current instance or an empty array if no value is
        ///    present.
        /// </value>
        /// <remarks>
        ///    This property is implemented using the ACTOR/PERFORMER stored in
        ///    the MKV Tag element.
        /// </remarks>
        public override string [] Performers
        {
            get { return Get(IsVideo ? "ACTOR" : "PERFORMER"); }
            set { Set(IsVideo ? "ACTOR" : "PERFORMER", null, value); }
        }

        /// <summary>
        ///    Gets and sets the sort names of the performers or artists
        ///    who performed in the media described by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string[]" /> containing the sort names for
        ///    the performers or artists who performed in the media
        ///    described by the current instance, or an empty array if
        ///    no value is present. 
        /// </value>
        /// <remarks>
        ///    This property is implemented using the nested Matroska 
        ///    SimpleTag "SORT_WITH" inside the "ACTOR" or "PERFORMER" SimpleTag.
        /// </remarks>
        public override string [] PerformersSort
        {
            get { return Get(IsVideo ? "ACTOR" : "PERFORMER", "SORT_WITH"); }
            set { Set(IsVideo ? "ACTOR" : "PERFORMER", "SORT_WITH", value); }
        }


        /// <summary>
        ///    Gets and sets the role of the performers or artists
        ///    who performed in the media described by the current instance.
        ///    For an movie, this represents a character of an actor.
        ///    For a music, this may represent the instrument of the artist.
        /// </summary>
        /// <value>
        ///    A <see cref="string[]" /> containing the roles for
        ///    the performers or artists who performed in the media
        ///    described by the current instance, or an empty array if
        ///    no value is present. 
        /// </value>
        /// <remarks>
        ///    This property is implemented using the nested Matroska 
        ///    SimpleTag "CHARACTER" or "INSTRUMENTS" inside the 
        ///    "ACTOR" or "PERFORMER" SimpleTag.
        /// </remarks>
        public string[] PerformersRole
        {
            get { return Get(IsVideo ? "ACTOR" : "PERFORMER", IsVideo ? "CHARACTER" : "INSTRUMENTS"); }
            set { Set(IsVideo ? "ACTOR" : "PERFORMER", IsVideo ? "CHARACTER" : "INSTRUMENTS", value); }
        }


        /// <summary>
        ///    Gets and sets the band or artist who is credited in the
        ///    creation of the entire album or collection containing the
        ///    media described by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string[]" /> containing the band or artist
        ///    who is credited in the creation of the entire album or
        ///    collection containing the media described by the current
        ///    instance or an empty array if no value is present.
        /// </value>
        /// <remarks>
        ///    This property is implemented using the "ARTIST" Tag.
        /// </remarks>
        public override string [] AlbumArtists
        {
            get { return TagsAlbum(false)?.Get("ARTIST"); }
            set { TagsAlbum(true)?.Set("ARTIST", null, value); }
        }

        /// <summary>
        ///    Gets and sets the sort names for the band or artist who
        ///    is credited in the creation of the entire album or
        ///    collection containing the media described by the
        ///    current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string[]" /> containing the sort names
        ///    for the band or artist who is credited in the creation
        ///    of the entire album or collection containing the media
        ///    described by the current instance or an empty array if
        ///    no value is present.
        /// </value>
        /// <remarks>
        ///    This property is implemented using the nested Matroska 
        ///    SimpleTag "SORT_WITH" inside the "ARTIST" SimpleTag.
        /// </remarks>

        public override string [] AlbumArtistsSort
        {
            get { return TagsAlbum(false)?.Get("ARTIST", "SORT_WITH"); }
            set { TagsAlbum(true)?.Set("ARTIST", "SORT_WITH", value); }
        }

        /// <summary>
        ///    Gets and sets the composers of the media represented by
        ///    the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string[]" /> containing the composers of the
        ///    media represented by the current instance or an empty
        ///    array if no value is present.
        /// </value>
        /// <remarks>
        ///    This property is implemented using the "COMPOSER" Tag.
        /// </remarks>
        public override string [] Composers
        {
            get { return Get("COMPOSER"); }
            set { Set("COMPOSER", null, value); }
        }


        /// <summary>
        ///    Gets and sets the sort names for the composers of the 
        ///    media represented by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string[]" /> containing the sort names
        ///    for the composers of the media represented by the 
        ///    current instance or an empty array if no value is present.
        /// </value>
        /// <remarks>
        ///    <para>This field is typically optional but aids in the
        ///    sorting of compilations or albums with multiple Composers.
        ///    </para>
        ///    <para>As this value is to be used as a sorting key, it
        ///    should be used with less variation than <see
        ///    cref="Composers" />. Where performers can be broken into
        ///    muliple artist it is best to stick with a single composer.
        ///    For example, "McCartney, Paul".</para>
        /// </remarks>
        /// <remarks>
        ///    This property is implemented using the nested Matroska 
        ///    SimpleTag "SORT_WITH" inside the "COMPOSER" SimpleTag.
        /// </remarks>
        public override string[] ComposersSort
        {
            get { return Get("COMPOSER", "SORT_WITH"); }
            set { Set("COMPOSER", "SORT_WITH", value); }
        }


        /// <summary>
        ///    Gets and sets the album of the media represented by the
        ///    current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> object containing the album of
        ///    the media represented by the current instance or <see
        ///    langword="null" /> if no value is present.
        /// </value>
        /// <remarks>
        ///    This property is implemented using the "TITLE" Tag in the Collection Tags.
        /// </remarks>
        public override string Album
        {
            get { return TagsAlbum(false)?.GetString("TITLE"); }
            set { TagsAlbum(true)?.Set("TITLE", null, value); }
        }

        /// <summary>
        ///    Gets and sets the sort names for the Album Title of
        ///    the media described by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> containing the sort name of 
        ///    the Album Title of the media described by the current
        ///    instance or null if no value is present.
        /// </value>
        /// <remarks>
        ///    This property is implemented using the nested Matroska 
        ///    SimpleTag "SORT_WITH" inside the "TITLE" SimpleTag.
        /// </remarks>
        public override string AlbumSort
        {
            get { return TagsAlbum(false)?.GetString("TITLE", "SORT_WITH"); }
            set { TagsAlbum(true)?.Set("TITLE", "SORT_WITH", value); }
        }

        /// <summary>
        ///    Gets and sets a user comment on the media represented by
        ///    the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> object containing user comments
        ///    on the media represented by the current instance or <see
        ///    langword="null" /> if no value is present.
        /// </value>
        /// <remarks>
        ///    This property is implemented using the "COMMENT" Tag.
        /// </remarks>
        public override string Comment
        {
            get { return GetString("COMMENT"); }
            set { Set("COMMENT", null, value); }
        }

        /// <summary>
        ///    Gets and sets the genres of the media represented by the
        ///    current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string[]" /> containing the genres of the
        ///    media represented by the current instance or an empty
        ///    array if no value is present.
        /// </value>
        /// <remarks>
        ///    This property is implemented using the "GENRE" Tag.
        /// </remarks>
        public override string [] Genres
        {
            get
            {
                string value = GetString("GENRE");

                if (value == null || value.Trim ().Length == 0)
                    return new string [] { };

                string [] result = value.Split (';');

                for (int i = 0; i < result.Length; i++) {
                    string genre = result [i].Trim ();

                    byte genre_id;
                    int closing = genre.IndexOf (')');
                    if (closing > 0 && genre [0] == '(' &&
                        byte.TryParse (genre.Substring (
                        1, closing - 1), out genre_id))
                        genre = TagLib.Genres
                            .IndexToAudio (genre_id);

                    result [i] = genre;
                }

                return result;
            }
            set
            {
                Set("GENRE", null, String.Join ("; ", value));
            }
        }

        /// <summary>
        ///    Gets and sets the year that the media represented by the
        ///    current instance was recorded.
        /// </summary>
        /// <value>
        ///    A <see cref="uint" /> containing the year that the media
        ///    represented by the current instance was created or zero
        ///    if no value is present.
        /// </value>
        public override uint Year
        {
            get
            {
                string val = GetString("DATE_RECORDED");
                uint ret = 0;

                // Parse Date to retrieve year
                // Expected format: YYYY-MM-DD HH:MM:SS.MSS 
                //   with: YYYY = Year, -MM = Month, -DD = Days, 
                //         HH = Hours, :MM = Minutes, :SS = Seconds, :MSS = Milliseconds
                if (val != null)
                {
                    int off = val.IndexOf('-');
                    if (off > 0) val = val.Substring(0, off);
                    uint.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out ret);
                }

                return ret;
            }
            set { Set("DATE_RECORDED", null, value); }
        }

        /// <summary>
        ///    Gets and sets the position of the media represented by
        ///    the current instance in its containing item (album, disc, episode, collection...).
        /// </summary>
        /// <value>
        ///    A <see cref="uint" /> containing the position of the
        ///    media represented by the current instance in its
        ///    containing album or zero if not specified.
        /// </value>
        public override uint Track
        {
            get { return GetUint("PART_NUMBER"); }
            set { Set("PART_NUMBER", null, value, "00"); }
        }

        /// <summary>
        ///    Gets and sets the number of items contained in the parent Tag (album, disc, episode, collection...)
        ///    the media represented by the current instance belongs to.
        /// </summary>
        /// <value>
        ///    A <see cref="uint" /> containing the number of tracks in
        ///    the album containing the media represented by the current
        ///    instance or zero if not specified.
        /// </value>
        public override uint TrackCount
        {
            get { return TagsGet(false, (ushort)(TargetTypeValue + 10))?.GetUint("TOTAL_PARTS") ?? 0; }
            set { TagsGet(true, (ushort)(TargetTypeValue + 10))?.Set("TOTAL_PARTS", null, value); }
        }

        /// <summary>
        ///    Gets and sets the number of the disc containing the media
        ///    represented by the current instance in the boxed set.
        /// </summary>
        /// <value>
        ///    A <see cref="uint" /> containing the number of the disc
        ///    containing the media represented by the current instance
        ///    in the boxed set.
        /// </value>
        public override uint Disc
        {
            get { return TagsGet(false, 40)?.GetUint("PART_NUMBER") ?? 0; }
            set { TagsGet(true, 40)?.Set("PART_NUMBER", null, value); }
        }

        /// <summary>
        ///    Gets and sets the number of discs in the boxed set
        ///    containing the media represented by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="uint" /> containing the number of discs in
        ///    the boxed set containing the media represented by the
        ///    current instance or zero if not specified.
        /// </value>
        public override uint DiscCount
        {
            get { return TagsGet(false, 50)?.GetUint("TOTAL_PARTS") ?? 0; }
            set { TagsGet(true, 50)?.Set("TOTAL_PARTS", null, value); }
        }

        /// <summary>
        ///    Gets and sets the lyrics or script of the media
        ///    represented by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> object containing the lyrics or
        ///    script of the media represented by the current instance
        ///    or <see langword="null" /> if no value is present.
        /// </value>
        public override string Lyrics
        {
            get { return GetString("LYRICS"); }
            set { Set("LYRICS", null, value); }
        }

        /// <summary>
        ///    Gets and sets the grouping on the album which the media
        ///    in the current instance belongs to.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> object containing the grouping on
        ///    the album which the media in the current instance belongs
        ///    to or <see langword="null" /> if no value is present.
        /// </value>
        public override string Grouping
        {
            get { return TagsAlbum(false)?.GetString("GROUPING"); }
            set { TagsAlbum(true)?.Set("GROUPING", null, value); }
        }

        /// <summary>
        ///    Gets and sets the number of beats per minute in the audio
        ///    of the media represented by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="uint" /> containing the number of beats per
        ///    minute in the audio of the media represented by the
        ///    current instance, or zero if not specified.
        /// </value>
        public override uint BeatsPerMinute
        {
            get { return GetUint("BPM", null, true); }
            set { Set("BPM", null, value); }
        }

        /// <summary>
        ///    Gets and sets the conductor or director of the media
        ///    represented by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> object containing the conductor
        ///    or director of the media represented by the current
        ///    instance or <see langword="null" /> if no value present.
        /// </value>
        public override string Conductor
        {
            get { return GetString(IsVideo ? "DIRECTOR" : "CONDUCTOR"); }
            set { Set(IsVideo ? "DIRECTOR" : "CONDUCTOR", null, value); }
        }

        /// <summary>
        ///    Gets and sets the copyright information for the media
        ///    represented by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> object containing the copyright
        ///    information for the media represented by the current
        ///    instance or <see langword="null" /> if no value present.
        /// </value>
        /// <remarks>
        ///    This property is implemented using the "COPYRIGHT" Tag.
        /// </remarks>
        public override string Copyright
        {
            get { return GetString("COPYRIGHT"); }
            set { Set("COPYRIGHT", null, value); }
        }

        /// <summary>
        ///    Gets and sets the MusicBrainz Artist ID of
        ///    the media described by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> containing the MusicBrainz 
        ///    ArtistID for the media described by the current
        ///    instance or null if no value is present.
        /// </value>
        public override string MusicBrainzArtistId
        {
            get { return null; }
            set { }
        }

        /// <summary>
        ///    Gets and sets the MusicBrainz Release ID of
        ///    the media described by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> containing the MusicBrainz 
        ///    ReleaseID for the media described by the current
        ///    instance or null if no value is present.
        /// </value>
        public override string MusicBrainzReleaseId
        {
            get { return null; }
            set { }
        }

        /// <summary>
        ///    Gets and sets the MusicBrainz Release Artist ID of
        ///    the media described by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> containing the MusicBrainz 
        ///    ReleaseArtistID for the media described by the current
        ///    instance or null if no value is present.
        /// </value>
        public override string MusicBrainzReleaseArtistId
        {
            get { return null; }
            set { }
        }

        /// <summary>
        ///    Gets and sets the MusicBrainz Track ID of
        ///    the media described by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> containing the MusicBrainz 
        ///    TrackID for the media described by the current
        ///    instance or null if no value is present.
        /// </value>
        public override string MusicBrainzTrackId
        {
            get { return null; }
            set { }
        }

        /// <summary>
        ///    Gets and sets the MusicBrainz Disc ID of
        ///    the media described by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> containing the MusicBrainz 
        ///    DiscID for the media described by the current
        ///    instance or null if no value is present.
        /// </value>
        public override string MusicBrainzDiscId
        {
            get { return null; }
            set { }
        }

        /// <summary>
        ///    Gets and sets the MusicIP PUID of
        ///    the media described by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> containing the MusicIPPUID 
        ///    for the media described by the current instance or
        ///    null if no value is present.
        /// </value>
        public override string MusicIpId
        {
            get { return null; }
            set { }
        }

        // <summary>
        //    Gets and sets the AmazonID of
        //    the media described by the current instance.
        // </summary>
        // <value>
        //    A <see cref="string" /> containing the AmazonID 
        //    for the media described by the current instance or
        //    null if no value is present.  
        // </value>
        // <remarks>
        //    A definition on where to store the ASIN for
        //    Windows Media is not currently defined
        // </remarks>
        //public override string AmazonId {
        //    get { return null; }
        //    set {}
        //}

        /// <summary>
        ///    Gets and sets the MusicBrainz Release Status of
        ///    the media described by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> containing the MusicBrainz 
        ///    ReleaseStatus for the media described by the current
        ///    instance or null if no value is present.
        /// </value>
        public override string MusicBrainzReleaseStatus
        {
            get { return null; }
            set { }
        }

        /// <summary>
        ///    Gets and sets the MusicBrainz Release Type of
        ///    the media described by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> containing the MusicBrainz 
        ///    ReleaseType for the media described by the current
        ///    instance or null if no value is present.
        /// </value>
        public override string MusicBrainzReleaseType
        {
            get { return null; }
            set { }
        }

        /// <summary>
        ///    Gets and sets the MusicBrainz Release Country of
        ///    the media described by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="string" /> containing the MusicBrainz 
        ///    ReleaseCountry for the media described by the current
        ///    instance or null if no value is present.
        /// </value>
        public override string MusicBrainzReleaseCountry
        {
            get { return null; }
            set { }
        }

        /// <summary>
        ///    Gets and sets a collection of pictures associated with
        ///    the media represented by the current instance.
        /// </summary>
        /// <value>
        ///    A <see cref="IPicture[]" /> containing a collection of
        ///    pictures associated with the media represented by the
        ///    current instance or an empty array if none are present.
        /// </value>
        public override IPicture [] Pictures
        {
            get
            {
                return Tags?.Pictures;
            }
            set
            {
                Tags.Pictures = value;
            }
        }

        /// <summary>
        ///    Gets whether or not the current instance is empty.
        /// </summary>
        /// <value>
        ///    <see langword="true" /> if the current instance does not
        ///    any values. Otherwise <see langword="false" />.
        /// </value>
        public override bool IsEmpty
        {
            get
            {
                return SimpleTags.Count == 0;
            }
        }

        /// <summary>
        ///    Clears the values stored in the current instance.
        /// </summary>
        public override void Clear ()
        {
            SimpleTags.Clear();
        }

        #endregion
    }
}

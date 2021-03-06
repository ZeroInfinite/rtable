﻿// azure-rtable ver. 0.9
//
// Copyright (c) Microsoft Corporation
//
// All rights reserved.
//
// MIT License
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files
// (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify,
// merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished
// to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


namespace Microsoft.Azure.Toolkit.Replication
{
    using System;
    using System.Runtime.Serialization;
    using System.Collections.Generic;
    using System.Linq;

    [DataContract(Namespace = "http://schemas.microsoft.com/windowsazure")]
    public class ReplicatedTableConfiguredTable
    {
        [DataMember(IsRequired = true)]
        public string TableName { get; set; }

        [DataMember(IsRequired = true)]
        public string ViewName { get; set; }

        [DataMember(IsRequired = true)]
        public bool ConvertToRTable { get; set; }

        /// <summary>
        /// If true, this config is used as default for other tables.
        /// In that case, TableName doesn't necessarly map to a real table name, can be generic name such "*" ...
        /// </summary>
        [DataMember(IsRequired = true)]
        public bool UseAsDefault { get; set; }

        [DataMember]
        public string PartitionOnProperty;

        [DataMember]
        public Dictionary<string, string> PartitionsToViewMap;

        /// <summary>
        /// Returns True if the table refers that view
        /// </summary>
        /// <param name="viewName"></param>
        /// <returns></returns>
        internal protected bool IsViewReferenced(string viewName)
        {
            if (string.IsNullOrEmpty(viewName))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(ViewName) &&
                ViewName.Equals(viewName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Check partition map
            if (string.IsNullOrEmpty(PartitionOnProperty) ||
                PartitionsToViewMap == null)
            {
                return false;
            }

            KeyValuePair<string, string>
            entry = PartitionsToViewMap.FirstOrDefault(x => x.Value.Equals(viewName, StringComparison.OrdinalIgnoreCase));

            if (entry.Equals(default(KeyValuePair<string, string>)))
            {
                return false;
            }

            return !string.IsNullOrEmpty(entry.Key);
        }

        /// <summary>
        /// Returns True if any view is null or empty
        /// </summary>
        /// <returns></returns>
        internal protected bool IsAnyViewNullOrEmpty()
        {
            if (string.IsNullOrEmpty(ViewName))
            {
                return true;
            }

            /*
             * Key = ""  - View = ""         => Ignore
             * Key = ""  - View = "viewName" => Ignore
             * Key = "X" - View = ""         => TRUE
             * Key = "Y" - View = "viewName" => FALSE
             */
            return PartitionsToViewMap != null &&
                   PartitionsToViewMap.Where(e => !string.IsNullOrEmpty(e.Key))
                                      .Select(entry => entry.Value)
                                      .Any(string.IsNullOrEmpty);
        }

        /// <summary>
        /// Returns the view to use for a given partition value (case insensitive)
        /// If partition is null or not found in the map we'll use the default view
        /// </summary>
        /// <param name="partition"></param>
        /// <returns></returns>
        internal protected string GetViewForPartition(string partition = null)
        {
            if (string.IsNullOrEmpty(partition))
            {
                return ViewName;
            }

            if (!IsTablePartitioned())
            {
                return ViewName;
            }

            KeyValuePair<string, string>
            entry = PartitionsToViewMap.FirstOrDefault(x => x.Key.Equals(partition, StringComparison.OrdinalIgnoreCase));

            if(entry.Equals(default(KeyValuePair<string, string>)))
            {
                return ViewName;
            }

            return string.IsNullOrEmpty(entry.Value)
                    ? ViewName
                    : entry.Value;
        }

        /// <summary>
        /// Returns True if table is partitioned i.e.
        /// - partitioning is enabled and
        /// - we have at least one valid partition entry (key != NullOrEmpty, value != NullOrEmpty)
        /// </summary>
        /// <returns></returns>
        internal protected bool IsTablePartitioned()
        {
            if (string.IsNullOrEmpty(PartitionOnProperty) || PartitionsToViewMap == null)
            {
                return false;
            }

            return PartitionsToViewMap.Where(e => !string.IsNullOrEmpty(e.Key))
                                      .Select(entry => entry.Value)
                                      .Any(v => !string.IsNullOrEmpty(v));
        }
    }
}

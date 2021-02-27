﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;

namespace MarkMpn.Sql4Cds.Engine.ExecutionPlan
{
    /// <summary>
    /// Returns only one entity per unique combinatioh of values in specified columns
    /// </summary>
    public class DistinctNode : BaseNode
    {
        class DistinctKey
        {
            private List<object> _values;
            private readonly int _hashCode;

            public DistinctKey(Entity entity, List<string> columns)
            {
                _values = columns.Select(col => entity[col]).ToList();

                _hashCode = 0;

                foreach (var val in _values)
                {
                    if (val == null)
                        continue;

                    _hashCode ^= StringComparer.CurrentCultureIgnoreCase.GetHashCode(val);
                }
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }

            public override bool Equals(object obj)
            {
                var other = (DistinctKey)obj;

                for (var i = 0; i < _values.Count; i++)
                {
                    if (_values[i] == null && other._values[i] == null)
                        continue;

                    if (_values[i] == null || other._values[i] == null)
                        return false;

                    if (StringComparer.CurrentCultureIgnoreCase.Compare(_values[i], other._values[i]) != 0)
                        return false;
                }

                return true;
            }
        }
        /// <summary>
        /// The columns to consider
        /// </summary>
        public List<string> Columns { get; } = new List<string>();

        /// <summary>
        /// The data source to take the values from
        /// </summary>
        public IExecutionPlanNode Source { get; set; }

        protected override IEnumerable<Entity> ExecuteInternal(IOrganizationService org, IAttributeMetadataCache metadata, IQueryExecutionOptions options, IDictionary<string, Type> parameterTypes, IDictionary<string, object> parameterValues)
        {
            var distinct = new HashSet<DistinctKey>();

            foreach (var entity in Source.Execute(org, metadata, options, parameterTypes, parameterValues))
            {
                var key = new DistinctKey(entity, Columns);

                if (distinct.Add(key))
                    yield return entity;
            }
        }

        public override NodeSchema GetSchema(IAttributeMetadataCache metadata, IDictionary<string, Type> parameterTypes)
        {
            return Source.GetSchema(metadata, parameterTypes);
        }

        public override IEnumerable<IExecutionPlanNode> GetSources()
        {
            yield return Source;
        }

        public override IExecutionPlanNode FoldQuery(IAttributeMetadataCache metadata, IQueryExecutionOptions options, IDictionary<string, Type> parameterTypes)
        {
            Source = Source.FoldQuery(metadata, options, parameterTypes);

            if (Source is FetchXmlScan fetch)
            {
                fetch.FetchXml.distinct = true;
                fetch.FetchXml.distinctSpecified = true;
                return fetch;
            }

            return this;
        }

        public override void AddRequiredColumns(IAttributeMetadataCache metadata, IDictionary<string, Type> parameterTypes, IList<string> requiredColumns)
        {
            foreach (var col in Columns)
            {
                if (!requiredColumns.Contains(col, StringComparer.OrdinalIgnoreCase))
                    requiredColumns.Add(col);
            }

            Source.AddRequiredColumns(metadata, parameterTypes, requiredColumns);
        }
    }
}

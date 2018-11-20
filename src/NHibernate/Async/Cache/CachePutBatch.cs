﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NHibernate.Engine;

namespace NHibernate.Cache
{
	using System.Threading.Tasks;
	using System.Threading;
	internal partial class CachePutBatch : AbstractCacheBatch<CachePutData>
	{

		protected override async Task ExecuteAsync(CachePutData[] data, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var length = data.Length;
			var keys = new CacheKey[length];
			var values = new object[length];
			var versions = new object[length];
			var versionComparers = new IComparer[length];
			var minimalPuts = new bool[length];

			for (int i = 0; i < length; i++)
			{
				var item = data[i];
				keys[i] = item.Key;
				values[i] = item.Value;
				versions[i] = item.Version;
				versionComparers[i] = item.VersionComparer;
				minimalPuts[i] = item.MinimalPut;
			}

			var factory = Session.Factory;
			var cacheStrategy = CacheConcurrencyStrategy;
			var puts = await (cacheStrategy.PutManyAsync(keys, values, Session.Timestamp, versions, versionComparers, minimalPuts, cancellationToken)).ConfigureAwait(false);

			if (factory.Statistics.IsStatisticsEnabled && puts.Any(o => o))
			{
				factory.StatisticsImplementor.SecondLevelCachePut(cacheStrategy.RegionName);
			}
		}
	}
}
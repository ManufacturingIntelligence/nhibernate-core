﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


namespace NHibernate.Collection
{
	using System.Threading.Tasks;
	using System.Threading;
	public partial interface ILazyInitializedCollection
	{

		/// <summary>
		/// Force immediate initialization.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the work</param>
		Task ForceInitializationAsync(CancellationToken cancellationToken);

	}
}

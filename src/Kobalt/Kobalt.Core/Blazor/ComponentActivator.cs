using Microsoft.AspNetCore.Components;

namespace YumeChan.NetRunner.Infrastructure.Blazor
{
	public sealed class ComponentActivator : IComponentActivator
	{
		private readonly IServiceProvider _container;

		public ComponentActivator(IServiceProvider container)
		{
			this._container = container;
		}

		public IComponent CreateInstance(Type type)
		{
			object? component = _container.GetService(type) ?? Activator.CreateInstance(type);
			return (IComponent)component ?? throw new InvalidOperationException($"Cannot create an instance of {type}.");
		}
	}
}

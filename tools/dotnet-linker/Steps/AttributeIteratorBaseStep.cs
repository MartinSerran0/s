using System;
using System.Collections;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;
using Xamarin.Tuner;

#nullable enable

namespace Xamarin.Linker.Steps {

	public abstract class AttributeIteratorBaseStep : BaseSubStep {

		protected DerivedLinkContext LinkContext {
			get {
				return LinkerConfiguration.GetInstance (Context).DerivedLinkContext;
			}
		}

		public override SubStepTargets Targets {
			get {
				return SubStepTargets.Assembly
					| SubStepTargets.Type
					| SubStepTargets.Field
					| SubStepTargets.Method
					| SubStepTargets.Property
					| SubStepTargets.Event;
			}
		}

		public override bool IsActiveFor (AssemblyDefinition assembly)
		{
			return Annotations.GetAction (assembly) == AssemblyAction.Link;
		}

		public override void ProcessAssembly (AssemblyDefinition assembly)
		{
			ProcessAttributeProvider (assembly);
			ProcessAttributeProvider (assembly.MainModule);
		}

		public override void ProcessType (TypeDefinition type)
		{
			ProcessAttributeProvider (type);

			if (type.HasGenericParameters)
				ProcessAttributeProviderCollection (type.GenericParameters);
		}

		void ProcessAttributeProviderCollection (IList list)
		{
			for (int i = 0; i < list.Count; i++)
				ProcessAttributeProvider ((ICustomAttributeProvider) list [i]!);
		}

		public override void ProcessField (FieldDefinition field)
		{
			ProcessAttributeProvider (field);
		}

		public override void ProcessMethod (MethodDefinition method)
		{
			ProcessMethodAttributeProvider (method);
		}

		void ProcessMethodAttributeProvider (MethodDefinition method)
		{
			ProcessAttributeProvider (method);
			ProcessAttributeProvider (method.MethodReturnType);

			if (method.HasParameters)
				ProcessAttributeProviderCollection (method.Parameters);

			if (method.HasGenericParameters)
				ProcessAttributeProviderCollection (method.GenericParameters);
		}

		public override void ProcessProperty (PropertyDefinition property)
		{
			ProcessAttributeProvider (property);
		}

		public override void ProcessEvent (EventDefinition @event)
		{
			ProcessAttributeProvider (@event);
		}

		void ProcessAttributeProvider (ICustomAttributeProvider provider)
		{
			if (!provider.HasCustomAttributes)
				return;

			for (int i = 0; i < provider.CustomAttributes.Count; i++) {
				var attrib = provider.CustomAttributes [i];
				ProcessAttribute (provider, attrib, out var remove);

				if (remove)
					provider.CustomAttributes.RemoveAt (i--);
			}
		}

		protected abstract void ProcessAttribute (ICustomAttributeProvider provider, CustomAttribute attribute, out bool remove);
	}
}

﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shiandow.Lending;
using Mpdn.Extensions.Framework.Filter;
using Mpdn.Extensions.Framework.RenderChain.Filters;
using SharpDX;

// ReSharper disable once CheckNamespace
namespace Mpdn.Extensions.Framework.RenderChain
{
    using TransformFunc = Func<TextureSize, TextureSize>;
    using static Shiandow.Lending.LendableHelper;

    #region Interfaces

    public interface ITaggableFilter
    {
        void EnableTag();
    }

    public interface ITextureFilter<out TTexture> : IFilter<ITextureDescription, TTexture>
       where TTexture : IBaseTexture
    { }

    public interface ITextureFilter : ITextureFilter<ITexture2D> { }

    public interface IResizeableFilter : ITextureFilter, ITaggableFilter
    {
        ITextureFilter SetSize(TextureSize outputSize);
    }

    public interface IOffsetFilter : ITextureFilter
    {
        ITextureFilter ForceOffsetCorrection();
    }

    public interface ICompositionFilter : IResizeableFilter
    {
        ITextureFilter Luma { get; }
        ITextureFilter Chroma { get; }
        TextureSize TargetSize { get; }
        Vector2 ChromaOffset { get; }
    }

    public interface IManagedTexture<out TTexture> : ILendable<ITextureOutput<TTexture>>
        where TTexture : IBaseTexture
    {
        bool Valid { get; }
    }

    public interface IChromaScaler
    {
        ITextureFilter ScaleChroma(ICompositionFilter composition);
    }

    #endregion

    #region Classes

    public class ArgumentList : IEnumerable<KeyValuePair<string, ArgumentList.Entry>>
    {
        private readonly IDictionary<string, Entry> m_Arguments;

        public ArgumentList()
        {
            m_Arguments = new Dictionary<string, Entry>();
        }

        public ArgumentList(IDictionary<string, Entry> arguments)
        {
            m_Arguments = arguments;
        }

        public ArgumentList(ArgumentList argumentList)
        {
            m_Arguments = argumentList.ToDictionary(x => x.Key, x => x.Value);
        }

        #region Implementation 

        // Allow shader arguments to be accessed individually
        public Entry this[string identifier]
        {
            get { return m_Arguments[identifier]; }
            set { m_Arguments[identifier] = value; }
        }

        public ArgumentList Merge(ArgumentList other)
        {
            var dict = new Dictionary<String, Entry>(m_Arguments);
            foreach (var pair in other)
                dict[pair.Key] = pair.Value;

            return dict;
        }

        public IEnumerator<KeyValuePair<string, Entry>> GetEnumerator()
        {
            return m_Arguments.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private int m_NKeys = 0;

        private string NextKey()
        {
            return string.Format("args{0}", m_NKeys++);
        }

        #endregion

        #region Operators

        public void Add(Entry entry)
        {
            m_Arguments.Add(NextKey(), entry);
        }

        public void Add(string Key, Entry entry)
        {
            m_Arguments.Add(Key, entry);
        }

        public static implicit operator ArgumentList(Dictionary<string, Entry> arguments)
        {
            return new ArgumentList((IDictionary<string, Entry>)arguments);
        }

        public static implicit operator ArgumentList(Dictionary<string, Vector4> arguments)
        {
            return arguments.ToDictionary(x => x.Key, x => (Entry)x.Value);
        }

        public static implicit operator ArgumentList(Dictionary<string, Vector3> arguments)
        {
            return arguments.ToDictionary(x => x.Key, x => (Entry)x.Value);
        }

        public static implicit operator ArgumentList(Dictionary<string, Vector2> arguments)
        {
            return arguments.ToDictionary(x => x.Key, x => (Entry)x.Value);
        }

        public static implicit operator ArgumentList(Dictionary<string, float> arguments)
        {
            return arguments.ToDictionary(x => x.Key, x => (Entry)x.Value);
        }

        public static implicit operator ArgumentList(float[] arguments)
        {
            var list = new ArgumentList();
            for (var i = 0; 4 * i < arguments.Length; i++)
                list.Add(new Vector4(
                    arguments.ElementAtOrDefault(4 * i),
                    arguments.ElementAtOrDefault(4 * i + 1),
                    arguments.ElementAtOrDefault(4 * i + 2),
                    arguments.ElementAtOrDefault(4 * i + 3)));

            return list;
        }

        #endregion

        #region Auxilary Types

        public struct Entry
        {
            private readonly Vector4 m_Value;

            private Entry(Vector4 value)
            {
                m_Value = value;
            }

            #region Operators

            public static implicit operator Vector4(Entry argument)
            {
                return argument.m_Value;
            }

            public static implicit operator Entry(Vector4 argument)
            {
                return new Entry(argument);
            }

            public static implicit operator Entry(Vector3 argument)
            {
                return new Vector4(argument, 0.0f);
            }

            public static implicit operator Entry(Vector2 argument)
            {
                return new Vector4(argument, 0.0f, 0.0f);
            }

            public static implicit operator Entry(float argument)
            {
                return new Vector4(argument);
            }

            #endregion
        }

        #endregion
    }

    public abstract class ChromaChain : RenderChain, IChromaScaler
    {
        public abstract ITextureFilter ScaleChroma(ICompositionFilter composition);

        protected sealed override ITextureFilter CreateFilter(ITextureFilter input)
        {
            var composition = input as ICompositionFilter;
            if (composition == null)
                return input;

            return ScaleChroma(composition);
        }
    }

    #endregion

    #region Helpers

    public static class TextureFilterHelper
    {
        public static TextureSize Size(this IFilterDescription<ITextureDescription> filter)
        {
            return filter.Output.Size;
        }

        public static TextureFormat Format(this IFilterDescription<ITextureDescription> filter)
        {
            return filter.Output.Format;
        }
    }

    public static class TransformationHelper
    {
        public static ITextureFilter ConvertToRgb(this ITextureFilter filter)
        {
            return ColorimetricHelper.ConvertToRgb(filter);
        }

        public static ITextureFilter ConvertToYuv(this ITextureFilter filter)
        {
            return ColorimetricHelper.ConvertToYuv(filter);
        }

        public static IResizeableFilter Transform(this IResizeableFilter filter, Func<ITextureFilter, ITextureFilter> transformation)
        {
            return new TransformedResizeableFilter(transformation, filter);
        }

        public static IResizeableFilter Resize(this ITextureFilter inputFilter, TextureSize outputSize, TextureChannels? channels = null, Vector2? offset = null, IScaler upscaler = null, IScaler downscaler = null, IScaler convolver = null, TextureFormat? outputFormat = null, bool tagged =  false)
        {
            var result = new ResizeFilter(inputFilter, outputSize, channels ?? TextureChannels.All, offset ?? Vector2.Zero, upscaler, downscaler, convolver, outputFormat);
            if (tagged)
                result.EnableTag();
            return result;
        }

        public static ITextureFilter Convolve(this ITextureFilter inputFilter, IScaler convolver, TextureChannels? channels = null, Vector2? offset = null, IScaler upscaler = null, IScaler downscaler = null, TextureFormat ? outputFormat = null)
        {
            return new ResizeFilter(inputFilter, inputFilter.Size(), channels ?? TextureChannels.All, offset ?? Vector2.Zero, upscaler, downscaler, convolver, outputFormat);
        }

        public static ITextureFilter SetSize(this ITextureFilter filter, TextureSize size, bool tagged = false)
        {
            ITextureFilter textureFilter;
            if (filter.Size() == size && (textureFilter = filter as ITextureFilter) != null)
                return textureFilter;

            var resizeable = (filter as IResizeableFilter) ?? new ResizeFilter(filter, size);
            if (tagged)
                resizeable.EnableTag();

            return resizeable.SetSize(size);
        }

        #region Auxilary class(es)

        private sealed class TransformedResizeableFilter : TextureFilter, IResizeableFilter
        {
            private readonly IResizeableFilter m_InputFilter;
            private readonly Func<ITextureFilter, ITextureFilter> m_Transformation;

            public TransformedResizeableFilter(Func<ITextureFilter, ITextureFilter> transformation, IResizeableFilter inputFilter)
                : base(transformation(inputFilter))
            {
                m_InputFilter = inputFilter;
                m_Transformation = transformation;
            }

            public void EnableTag()
            {
                m_InputFilter.EnableTag();
            }

            public ITextureFilter SetSize(TextureSize outputSize)
            {
                return new TransformedResizeableFilter(m_Transformation, m_InputFilter);
            }
        }

        #endregion
    }

    public static class ShaderFilterHelper
    {
        public static ITextureFilter ApplyTo(this IShaderConfig settings, params ITextureFilter<IBaseTexture>[] inputFilters)
        {
            return settings.GetHandle().ApplyTo(inputFilters);
        }

        public static ITextureFilter Apply(this ITextureFilter<IBaseTexture> filter, IShaderConfig settings)
        {
            return settings.ApplyTo(filter);
        }
    }

    public static class ManagedTextureHelpers
    {
        private class ManagedTexture<TTexture> : Lendable<JustTextureOutput<TTexture>>, IManagedTexture<TTexture>
            where TTexture : IBaseTexture
        {
            public ManagedTexture(TTexture value)
            {
                m_TextureOutput = new JustTextureOutput<TTexture>(value);
            }

            public bool Valid { get { return !m_Disposed; } }

            ILease<ITextureOutput<TTexture>> ILendable<ITextureOutput<TTexture>>.GetLease()
            {
                return GetLease();
            }

            #region Lendable Implementation

            private readonly JustTextureOutput<TTexture> m_TextureOutput;
            private bool m_Disposed = false;

            protected override JustTextureOutput<TTexture> Value { get { return m_TextureOutput; } }

            protected override void Allocate() { }

            protected override void Deallocate()
            {
                if (!m_Disposed)
                {
                    Value.Dispose();
                    m_Disposed = true;
                }
            }

            #endregion
        }

        private class ManagedTextureFilter<TTexture> : Filter<ITextureDescription, TTexture>, ITextureFilter<TTexture>
            where TTexture : IBaseTexture
        {
            public ManagedTextureFilter(IManagedTexture<TTexture> texture)
                : this(texture.GetLease())
            { }

            private ManagedTextureFilter(ILease<ITextureOutput<TTexture>> lease)
                : base(FilterBaseHelper.Return(lease).Map(x => x.Value))
            { }
        }

        public static IManagedTexture<TTexture> GetManaged<TTexture>(this TTexture texture) 
            where TTexture : IBaseTexture
        {
            return new ManagedTexture<TTexture>(texture);
        }

        public static ITextureFilter<TTexture> ToFilter<TTexture>(this IManagedTexture<TTexture> texture)
            where TTexture : IBaseTexture
        {
            return new ManagedTextureFilter<TTexture>(texture);
        }
    }

    public static class MergeHelper
    {
        public static ITextureFilter MergeWith(this ITextureFilter inputY, ITextureFilter inputUv)
        {
            return new TextureFilter(MergeProcesses.MergeY_UV.ApplyTo(inputY, inputUv));
        }

        public static ITextureFilter MergeWith(this ITextureFilter inputY, ITextureFilter inputU, ITextureFilter inputV)
        {
            return new TextureFilter(MergeProcesses.MergeY_U_V.ApplyTo(inputY, inputU, inputV));
        }
    }

    #endregion
}

using System;
using Mpdn.RenderScript.Scaler;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using SharpDX;
using TransformFunc = System.Func<System.Drawing.Size, System.Drawing.Size>;
using YAXLib;

namespace Mpdn.RenderScript
{
    public interface IRenderChain
    {
        IFilter CreateFilter(IFilter sourceFilter);
    }

    public abstract class RenderChain : IRenderChain
    {
        public abstract IFilter CreateFilter(IFilter sourceFilter);

        #region Shader Compilation

        protected virtual string ShaderPath
        {
            get { return GetType().Name; }
        }

        protected string ShaderDataFilePath
        {
            get
            {
                var asmPath = typeof(IRenderScript).Assembly.Location;
                return Path.Combine(Common.GetDirectoryName(asmPath), "RenderScripts", ShaderPath);
            }
        }

        protected IShader CompileShader(string shaderFileName)
        {
            return ShaderCache.CompileShader(Path.Combine(ShaderDataFilePath, shaderFileName));
        }

        #endregion
    }

    public class StaticChain : IRenderChain
    {
        private Func<IFilter, IFilter> Compiler;

        public StaticChain(Func<IFilter, IFilter> compiler)
        {
            Compiler = compiler;
        }

        public IFilter CreateFilter(IFilter sourceFilter)
        {
            return Compiler(sourceFilter);
        }
    }

    public class FilterChain {
        public IFilter Filter;

        public FilterChain(IFilter sourceFilter)
        {
            Filter = sourceFilter;
        }

        public void Add(IRenderChain renderChain)
        {
            Filter = renderChain.CreateFilter(Filter);
        }

        public Size OutputSize
        {
            get { return Filter.OutputSize; }
        }
    }

    public abstract class CombinedChain : RenderChain
    {
        protected abstract void BuildChain(FilterChain Chain);

        public override IFilter CreateFilter(IFilter sourceFilter) {
            var chain = new FilterChain(sourceFilter);
            BuildChain(chain);

            return chain.Filter;
        }

        #region Convenience functions

        protected bool IsDownscalingFrom(Size size)
        {
            return !IsNotScalingFrom(size) && !IsUpscalingFrom(size);
        }

        protected bool IsNotScalingFrom(Size size)
        {
            return size == Renderer.TargetSize;
        }

        protected bool IsUpscalingFrom(Size size)
        {
            var targetSize = Renderer.TargetSize;
            return targetSize.Width > size.Width || targetSize.Height > size.Height;
        }

        protected bool IsDownscalingFrom(FilterChain chain)
        {
            return IsDownscalingFrom(chain.OutputSize);
        }

        protected bool IsNotScalingFrom(FilterChain chain)
        {
            return IsNotScalingFrom(chain.OutputSize);
        }

        protected bool IsUpscalingFrom(FilterChain chain)
        {
            return IsUpscalingFrom(chain.OutputSize);
        }

        #endregion
    }
}
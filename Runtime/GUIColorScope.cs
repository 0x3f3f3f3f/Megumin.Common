using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Megumin
{
    public struct GUIColorScope : IDisposable
    {
        private bool m_Disposed;

        private Color m_PreviousColor;

        public GUIColorScope(Color newColor)
        {
            m_Disposed = false;
            m_PreviousColor = GUI.color;
            GUI.color = newColor;
        }

        public GUIColorScope(float r, float g, float b, float a = 1f)
            : this(new Color(r, g, b, a))
        {
        }

        public void Dispose()
        {
            if (!m_Disposed)
            {
                m_Disposed = true;
                GUI.color = m_PreviousColor;
            }
        }
    }

    public struct GUIBackgroundColorScope : IDisposable
    {
        private bool m_Disposed;

        private Color m_PreviousColor;

        public GUIBackgroundColorScope(Color newColor)
        {
            m_Disposed = false;
            m_PreviousColor = GUI.backgroundColor;
            GUI.backgroundColor = newColor;
        }

        public GUIBackgroundColorScope(float r, float g, float b, float a = 1f)
            : this(new Color(r, g, b, a))
        {
        }

        public void Dispose()
        {
            if (!m_Disposed)
            {
                m_Disposed = true;
                GUI.backgroundColor = m_PreviousColor;
            }
        }
    }
}




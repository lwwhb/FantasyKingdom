using System;
using UnityEngine;
namespace Unity.FantasyKingdom
{
    public class FKReadme : ScriptableObject
    {
        public Texture2D icon;
        public string title;
        public Section[] sections;
        public bool loadedLayout;

        [Serializable]
        public class Section
        {
            public string heading, text, linkText, url;
        }
    }
}
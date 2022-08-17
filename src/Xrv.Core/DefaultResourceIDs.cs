using System;

namespace Xrv.Core
{
    public static class DefaultResourceIDs
    {
        public static class Mrtk
        {
            public static class Materials
            {
                public static class Cursor
                {
                    public static Guid CursorPinch = new Guid("d12dec8a-90fd-410d-acc6-078da8c70bb4");

                    public static Guid CursorBase = new Guid("b65fb622-f2bc-42ef-a797-3a98cd7df438");
                }
            }

            public static class Prefabs
            {
                public static Guid PressableButtonPlated = new Guid("dabe9073-b1d1-42c5-99b9-ebc0cb4a7430");
            }

            public static class Samplers
            {
                public static Guid LinearWrapSampler = new Guid("79d9580a-094a-407f-8773-2a3d217de8e7");
            }

            public static class Textures
            {
                public static Guid line_dots_png = new Guid("cb80d2ef-6bcc-4dd3-8e62-636465021fba");
            }
        }

        public static class Prefabs
        {
            public static Guid BaseDialogContents = new Guid("f544117b-51ca-46af-894e-49a48094651f");

            public static Guid HandMenu = new Guid("5f574367-77c6-4d6d-bef2-4e822c52cbac");

            public static Guid TabControl = new Guid("c0a378d7-445f-46ad-a5d8-0865a2bc25b7");

            public static Guid TextButton = new Guid("69925256-16a0-4938-81fb-2810f6afa4eb");

            public static Guid Window = new Guid("76b973f8-d096-4af1-b6c4-a4899e97eb63");
        }
    }
}

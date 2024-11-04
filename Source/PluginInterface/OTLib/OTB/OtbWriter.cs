#region Licence
/**
* Copyright © 2014-2019 OTTools <https://github.com/ottools/ItemEditor/>
*
* This program is free software; you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation; either version 2 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License along
* with this program; if not, write to the Free Software Foundation, Inc.,
* 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/
#endregion

#region Using Statements
using ItemEditor;
using OTLib.Collections;
using OTLib.Server.Items;
using OTLib.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml; // Added for XML handling
#endregion

namespace OTLib.OTB
{
    public class OtbWriter
    {
        #region Constructor

        public OtbWriter(ServerItemList items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            this.Items = items;
        }

        #endregion

        #region Public Properties

        public ServerItemList Items { get; private set; }

        public int Count
        {
            get
            {
                return this.Items.Count;
            }
        }

        #endregion

        #region Public Methods

        public bool Write(string path)
        {
            try
            {
                using (BinaryTreeWriter writer = new BinaryTreeWriter(path))
                {
                    writer.WriteUInt32(0, false); // version, always 0

                    writer.CreateNode(0); // root node
                    writer.WriteUInt32(0, true); // flags, unused for root node

                    OtbVersionInfo vi = new OtbVersionInfo();

                    vi.MajorVersion = this.Items.MajorVersion;
                    vi.MinorVersion = this.Items.MinorVersion;
                    vi.BuildNumber = this.Items.BuildNumber + 1; // Modified BuildNumber
                    vi.CSDVersion = string.Format("OTB {0}.{1}.{2}-{3}.{4}",
                        vi.MajorVersion, vi.MinorVersion, vi.BuildNumber,
                        this.Items.ClientVersion / 100, this.Items.ClientVersion % 100);

                    MemoryStream ms = new MemoryStream();
                    BinaryWriter property = new BinaryWriter(ms);
                    property.Write(vi.MajorVersion);
                    property.Write(vi.MinorVersion);
                    property.Write(vi.BuildNumber);
                    byte[] CSDVersion = Encoding.ASCII.GetBytes(vi.CSDVersion);
                    Array.Resize(ref CSDVersion, 128);
                    property.Write(CSDVersion);

                    writer.WriteProp(RootAttribute.Version, property);

                    foreach (ServerItem item in this.Items.Items)
                    {
                        List<ServerItemAttribute> saveAttributeList = new List<ServerItemAttribute>
                        {
                            ServerItemAttribute.ServerID
                        };

                        if (item.Type != ServerItemType.Deprecated)
                        {
                            saveAttributeList.Add(ServerItemAttribute.ClientID);
                            saveAttributeList.Add(ServerItemAttribute.SpriteHash);

                            if (item.MinimapColor != 0)
                            {
                                saveAttributeList.Add(ServerItemAttribute.MinimaColor);
                            }

                            if (item.MaxReadWriteChars != 0)
                            {
                                saveAttributeList.Add(ServerItemAttribute.MaxReadWriteChars);
                            }

                            if (item.MaxReadChars != 0)
                            {
                                saveAttributeList.Add(ServerItemAttribute.MaxReadChars);
                            }

                            if (item.LightLevel != 0 || item.LightColor != 0)
                            {
                                saveAttributeList.Add(ServerItemAttribute.Light);
                            }

                            if (item.Type == ServerItemType.Ground)
                            {
                                saveAttributeList.Add(ServerItemAttribute.GroundSpeed);
                            }

                            if (item.StackOrder != TileStackOrder.None)
                            {
                                saveAttributeList.Add(ServerItemAttribute.StackOrder);
                            }

                            if (item.TradeAs != 0)
                            {
                                saveAttributeList.Add(ServerItemAttribute.TradeAs);
                            }

                            if (!string.IsNullOrEmpty(item.Name))
                            {
                                saveAttributeList.Add(ServerItemAttribute.Name);
                            }
                        }

                        switch (item.Type)
                        {
                            case ServerItemType.Container:
                                writer.CreateNode((byte)ServerItemGroup.Container);
                                break;
                            case ServerItemType.Fluid:
                                writer.CreateNode((byte)ServerItemGroup.Fluid);
                                break;
                            case ServerItemType.Ground:
                                writer.CreateNode((byte)ServerItemGroup.Ground);
                                break;
                            case ServerItemType.Splash:
                                writer.CreateNode((byte)ServerItemGroup.Splash);
                                break;
                            case ServerItemType.Deprecated:
                                writer.CreateNode((byte)ServerItemGroup.Deprecated);
                                break;
                            default:
                                writer.CreateNode((byte)ServerItemGroup.None);
                                break;
                        }

                        uint flags = GetFlags(item);
                        writer.WriteUInt32(flags, true);

                        foreach (ServerItemAttribute attribute in saveAttributeList)
                        {
                            switch (attribute)
                            {
                                case ServerItemAttribute.ServerID:
                                    property.Write((ushort)item.ID);
                                    writer.WriteProp(ServerItemAttribute.ServerID, property);
                                    break;
                                case ServerItemAttribute.TradeAs:
                                    property.Write((ushort)item.TradeAs);
                                    writer.WriteProp(ServerItemAttribute.TradeAs, property);
                                    break;
                                case ServerItemAttribute.ClientID:
                                    property.Write((ushort)item.ClientId);
                                    writer.WriteProp(ServerItemAttribute.ClientID, property);
                                    break;
                                case ServerItemAttribute.GroundSpeed:
                                    property.Write((ushort)item.GroundSpeed);
                                    writer.WriteProp(ServerItemAttribute.GroundSpeed, property);
                                    break;
                                case ServerItemAttribute.Name:
                                    property.Write(item.Name.ToCharArray());
                                    writer.WriteProp(ServerItemAttribute.Name, property);
                                    break;
                                case ServerItemAttribute.SpriteHash:
                                    property.Write(item.SpriteHash);
                                    writer.WriteProp(ServerItemAttribute.SpriteHash, property);
                                    break;
                                case ServerItemAttribute.MinimaColor:
                                    property.Write((ushort)item.MinimapColor);
                                    writer.WriteProp(ServerItemAttribute.MinimaColor, property);
                                    break;
                                case ServerItemAttribute.MaxReadWriteChars:
                                    property.Write((ushort)item.MaxReadWriteChars);
                                    writer.WriteProp(ServerItemAttribute.MaxReadWriteChars, property);
                                    break;
                                case ServerItemAttribute.MaxReadChars:
                                    property.Write((ushort)item.MaxReadChars);
                                    writer.WriteProp(ServerItemAttribute.MaxReadChars, property);
                                    break;
                                case ServerItemAttribute.Light:
                                    property.Write((ushort)item.LightLevel);
                                    property.Write((ushort)item.LightColor);
                                    writer.WriteProp(ServerItemAttribute.Light, property);
                                    break;
                                case ServerItemAttribute.StackOrder:
                                    property.Write((byte)item.StackOrder);
                                    writer.WriteProp(ServerItemAttribute.StackOrder, property);
                                    break;
                            }
                        }

                        writer.CloseNode();
                    }

                    writer.CloseNode();
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool WriteToXml(string path)
        {
            try
            {
                using (XmlWriter writer = XmlWriter.Create(path, new XmlWriterSettings { Indent = true }))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Items");

                    // Write version info as XML elements
                    writer.WriteStartElement("VersionInfo");
                    writer.WriteElementString("MajorVersion", this.Items.MajorVersion.ToString());
                    writer.WriteElementString("MinorVersion", this.Items.MinorVersion.ToString());
                    writer.WriteElementString("BuildNumber", (this.Items.BuildNumber + 1).ToString()); // Reflect the BuildNumber increment
                    writer.WriteElementString("CSDVersion",
                        string.Format("OTB {0}.{1}.{2}-{3}.{4}",
                        this.Items.MajorVersion, this.Items.MinorVersion, this.Items.BuildNumber + 1,
                        this.Items.ClientVersion / 100, this.Items.ClientVersion % 100));
                    writer.WriteEndElement();

                    // Iterate through each item and write attributes
                    foreach (ServerItem item in this.Items.Items)
                    {
                        writer.WriteStartElement("Item");
                        writer.WriteAttributeString("ID", item.ID.ToString());
                        writer.WriteElementString("Type", item.Type.ToString());
                        writer.WriteElementString("ClientID", item.ClientId.ToString());
                        writer.WriteElementString("Name", item.Name);
                        writer.WriteElementString("TradeAs", item.TradeAs.ToString());
                        writer.WriteElementString("GroundSpeed", item.GroundSpeed.ToString());
                        writer.WriteElementString("SpriteHash", BitConverter.ToString(item.SpriteHash));
                        writer.WriteElementString("MinimapColor", item.MinimapColor.ToString());
                        writer.WriteElementString("MaxReadWriteChars", item.MaxReadWriteChars.ToString());
                        writer.WriteElementString("MaxReadChars", item.MaxReadChars.ToString());
                        writer.WriteElementString("LightLevel", item.LightLevel.ToString());
                        writer.WriteElementString("LightColor", item.LightColor.ToString());
                        writer.WriteElementString("StackOrder", item.StackOrder.ToString());

                        // Write flags as individual XML elements
                        writer.WriteElementString("Unpassable", item.Unpassable.ToString().ToLower());
                        writer.WriteElementString("BlockMissiles", item.BlockMissiles.ToString().ToLower());
                        writer.WriteElementString("BlockPathfinder", item.BlockPathfinder.ToString().ToLower());
                        writer.WriteElementString("HasElevation", item.HasElevation.ToString().ToLower());
                        writer.WriteElementString("ForceUse", item.ForceUse.ToString().ToLower());
                        writer.WriteElementString("MultiUse", item.MultiUse.ToString().ToLower());
                        writer.WriteElementString("Pickupable", item.Pickupable.ToString().ToLower());
                        writer.WriteElementString("Movable", item.Movable.ToString().ToLower());
                        writer.WriteElementString("Stackable", item.Stackable.ToString().ToLower());
                        writer.WriteElementString("StackOrderFlag", (item.StackOrder != TileStackOrder.None).ToString().ToLower());
                        writer.WriteElementString("Readable", item.Readable.ToString().ToLower());
                        writer.WriteElementString("Rotatable", item.Rotatable.ToString().ToLower());
                        writer.WriteElementString("Hangable", item.Hangable.ToString().ToLower());
                        writer.WriteElementString("HookSouth", item.HookSouth.ToString().ToLower());
                        writer.WriteElementString("HookEast", item.HookEast.ToString().ToLower());
                        writer.WriteElementString("HasCharges", item.HasCharges.ToString().ToLower());
                        writer.WriteElementString("IgnoreLook", item.IgnoreLook.ToString().ToLower());
                        writer.WriteElementString("AllowDistanceRead", item.AllowDistanceRead.ToString().ToLower());
                        writer.WriteElementString("IsAnimation", item.IsAnimation.ToString().ToLower());
                        writer.WriteElementString("FullGround", item.FullGround.ToString().ToLower());

                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }


        // Helper method to calculate flags
        private uint GetFlags(ServerItem item)
        {
            uint flags = 0;
            if (item.Unpassable) flags |= (uint)ServerItemFlag.Unpassable;
            if (item.BlockMissiles) flags |= (uint)ServerItemFlag.BlockMissiles;
            if (item.BlockPathfinder) flags |= (uint)ServerItemFlag.BlockPathfinder;
            if (item.HasElevation) flags |= (uint)ServerItemFlag.HasElevation;
            if (item.ForceUse) flags |= (uint)ServerItemFlag.ForceUse;
            if (item.MultiUse) flags |= (uint)ServerItemFlag.MultiUse;
            if (item.Pickupable) flags |= (uint)ServerItemFlag.Pickupable;
            if (item.Movable) flags |= (uint)ServerItemFlag.Movable;
            if (item.Stackable) flags |= (uint)ServerItemFlag.Stackable;
            if (item.StackOrder != TileStackOrder.None) flags |= (uint)ServerItemFlag.StackOrder;
            if (item.Readable) flags |= (uint)ServerItemFlag.Readable;
            if (item.Rotatable) flags |= (uint)ServerItemFlag.Rotatable;
            if (item.Hangable) flags |= (uint)ServerItemFlag.Hangable;
            if (item.HookSouth) flags |= (uint)ServerItemFlag.HookSouth;
            if (item.HookEast) flags |= (uint)ServerItemFlag.HookEast;
            if (item.HasCharges) flags |= (uint)ServerItemFlag.ClientCharges;
            if (item.IgnoreLook) flags |= (uint)ServerItemFlag.IgnoreLook;
            if (item.AllowDistanceRead) flags |= (uint)ServerItemFlag.AllowDistanceRead;
            if (item.IsAnimation) flags |= (uint)ServerItemFlag.IsAnimation;
            if (item.FullGround) flags |= (uint)ServerItemFlag.FullGround;
            return flags;
        }

        #endregion
    }
}

using NUnit.Framework.Constraints;
using SoftMasking.Samples;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

[CustomPropertyDrawer(typeof(MyDictionary))]
public class MyDictionaryDrawer : DictionaryDrawer<MeshRenderer, VFXComponent.SpecialMaterial> { }

public abstract class DictionaryDrawer<TK, TV> : PropertyDrawer
{
    private SerializableDictionary<TK, TV> _Dictionary;
    private bool _Foldout;

    private const float kElementHeight = 20f;
    private const float kElementSpacing = 2f;

    // for adding new items to the dictionary
    private MeshRenderer _key;
    private Material _mat1;
    private Material _mat2;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        CheckInitialize(property, label);

        const float addElementHeight = kElementHeight * 4;

        if (_Foldout)
            return (_Dictionary.Count * kElementHeight) + addElementHeight;

        return addElementHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        CheckInitialize(property, label);

        //TODO add labels
        //TODO make prettier
        //https://forum.unity.com/threads/finally-a-serializable-dictionary-for-unity-extracted-from-system-collections-generic.335797/

        //EditorGUI.PropertyF

        var propertyRect1 = position;
        propertyRect1.y = kElementHeight * 0;
        propertyRect1.height = kElementHeight - kElementSpacing;
        _key = (MeshRenderer)EditorGUI.ObjectField(propertyRect1, _key, typeof(MeshRenderer), true);

        var propertyRect2 = position;
        propertyRect2.y = kElementHeight * 1;
        propertyRect2.height = kElementHeight - kElementSpacing;
        _mat1 = (Material)EditorGUI.ObjectField(propertyRect2, _mat1, typeof(Material), true);

        var propertyRect3 = position;
        propertyRect3.y = kElementHeight * 2;
        propertyRect3.height = kElementHeight - kElementSpacing;
        _mat2 = (Material)EditorGUI.ObjectField(propertyRect3, _mat2, typeof(Material), true);

        var foldoutRect = position;
        foldoutRect.y = kElementHeight * 3;
        foldoutRect.height = kElementHeight;
        foldoutRect.width -= 2 * kElementHeight;

        EditorGUI.BeginChangeCheck();
        _Foldout = EditorGUI.Foldout(foldoutRect, _Foldout, label, true);
        if (EditorGUI.EndChangeCheck())
            EditorPrefs.SetBool(label.text, _Foldout);

        var buttonRect = position;
        buttonRect.y = foldoutRect.y;
        buttonRect.x = position.width - kElementHeight + position.x - kElementSpacing;
        buttonRect.width = kElementHeight + kElementSpacing;
        buttonRect.height = kElementHeight - kElementSpacing;

        if (GUI.Button(buttonRect, new GUIContent("+", "Add item"), EditorStyles.miniButton))
        {
            AddNewItem();
            fieldInfo.SetValue(property.serializedObject.targetObject, _Dictionary);
            property.serializedObject.Update();
        }

        buttonRect.x -= kElementHeight - kElementSpacing;
        if (GUI.Button(buttonRect, new GUIContent("X", "Clear dictionary"), EditorStyles.miniButtonRight))
        {
            ClearDictionary();
            fieldInfo.SetValue(property.serializedObject.targetObject, _Dictionary);
            property.serializedObject.Update();
        }

        if (!_Foldout)
        {
            if (GUI.changed)
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            return;
        }
            

        for (int i = 0; i < _Dictionary.Count; i++)
        {
            var ele = _Dictionary.ElementAt(i);
            var key = ele.Key;
            var value = ele.Value;

            var keyRect = position;
            keyRect.y = kElementHeight * (i + 4); // offset by 4 elements already in drawer
            keyRect.height = kElementHeight - kElementSpacing;
            keyRect.width /= 3f;

            EditorGUI.BeginChangeCheck();
            var newKey = DoField(keyRect, typeof(TK), key);
            if (EditorGUI.EndChangeCheck())
            {
                try
                {
                    _Dictionary.Remove(key);
                    _Dictionary.Add(newKey, value);
                    fieldInfo.SetValue(property.serializedObject.targetObject, _Dictionary);
                    property.serializedObject.Update();
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                }
                break;
            }

            var valueRect = position;
            valueRect.x = keyRect.xMax + kElementSpacing;
            valueRect.y = keyRect.y;
            valueRect.width = position.width - keyRect.width - kElementHeight;
            EditorGUI.BeginChangeCheck();
            value = DoField(valueRect, typeof(TV), value);
            if (EditorGUI.EndChangeCheck())
            {
                _Dictionary[key] = value;
                fieldInfo.SetValue(property.serializedObject.targetObject, _Dictionary);
                property.serializedObject.Update();
                break;
            }

            var removeRect = valueRect;
            removeRect.x = valueRect.xMax + kElementSpacing;
            removeRect.width = kElementHeight;
            if (GUI.Button(removeRect, new GUIContent("x", "Remove item"), EditorStyles.miniButtonRight))
            {
                RemoveItem(key);
                fieldInfo.SetValue(property.serializedObject.targetObject, _Dictionary); 
                property.serializedObject.Update();
                break;
            }

        }

        if (GUI.changed)
            EditorUtility.SetDirty(property.serializedObject.targetObject);
    }

    private void RemoveItem(TK key)
    {
        _Dictionary.Remove(key);
    }

    private void CheckInitialize(SerializedProperty property, GUIContent label)
    {
        if (_Dictionary == null)
        {
            var target = property.serializedObject.targetObject;
            _Dictionary = fieldInfo.GetValue(target) as SerializableDictionary<TK, TV>;
            if (_Dictionary == null)
            {
                _Dictionary = new SerializableDictionary<TK, TV>();
                fieldInfo.SetValue(target, _Dictionary);
                property.serializedObject.Update();
            }

            _Foldout = EditorPrefs.GetBool(label.text);
        }

        fieldInfo.SetValue(property.serializedObject.targetObject, _Dictionary);
        property.serializedObject.Update();
        property.serializedObject.ApplyModifiedProperties();
    }

    private static readonly Dictionary<Type, Func<Rect, object, object>> _Fields =
        new()
        {
            { typeof(int), (rect, value) => EditorGUI.IntField(rect, (int)value) },
            { typeof(float), (rect, value) => EditorGUI.FloatField(rect, (float)value) },
            { typeof(string), (rect, value) => EditorGUI.TextField(rect, (string)value) },
            { typeof(bool), (rect, value) => EditorGUI.Toggle(rect, (bool)value) },
            { typeof(Vector2), (rect, value) => EditorGUI.Vector2Field(rect, GUIContent.none, (Vector2)value) },
            { typeof(Vector3), (rect, value) => EditorGUI.Vector3Field(rect, GUIContent.none, (Vector3)value) },
            { typeof(Bounds), (rect, value) => EditorGUI.BoundsField(rect, (Bounds)value) },
            { typeof(Rect), (rect, value) => EditorGUI.RectField(rect, (Rect)value) },
        };

    private static T DoField<T>(Rect rect, Type type, T value)
    {
        if (_Fields.TryGetValue(type, out Func<Rect, object, object> field))
            return (T)field(rect, value);

        if (type.IsEnum)
            return (T)(object)EditorGUI.EnumPopup(rect, (Enum)(object)value);

        if (typeof(UnityObject).IsAssignableFrom(type))
            return (T)(object)EditorGUI.ObjectField(rect, (UnityObject)(object)value, type, true);

        if (type == typeof(VFXComponent.SpecialMaterial))
        {
            // draw two fields
            var specialMaterial = (VFXComponent.SpecialMaterial)(object)value;

            var wireframeSolidRect = rect;
            wireframeSolidRect.height = kElementHeight - kElementSpacing;
            wireframeSolidRect.width /= 2f;
            specialMaterial.wireframeSolid = (Material)EditorGUI.ObjectField(wireframeSolidRect, specialMaterial.wireframeSolid, typeof(Material), true);

            var transparentRect = rect;
            transparentRect.x = wireframeSolidRect.xMax + 2;
            transparentRect.height = kElementHeight - kElementSpacing;
            transparentRect.width /= 2f;
            specialMaterial.transparent = (Material)EditorGUI.ObjectField(transparentRect, specialMaterial.transparent, typeof(Material), true);

            return (T)(object)specialMaterial;
        }

        Debug.Log("Type is not supported: " + type);
        return value;
    }

    private void ClearDictionary()
    {
        _Dictionary.Clear();
    }

    private void AddNewItem()
    {
        /*
        TK key;
        if (typeof(TK) == typeof(string))
            key = (TK)(object)"";
        else key = default(TK);

        var value = default(TV);
        */

        if (_key == null) throw new ArgumentNullException("MeshRendeer (Key) cannot be null");
        if (_mat1 == null) throw new ArgumentNullException("Material 1 (Wireframe Solid) cannot be null");
        if (_mat2 == null) throw new ArgumentNullException("Material 2 (Transparent) cannot be null");

        var key = (TK)(object)_key;
        var value = (TV)(object)new VFXComponent.SpecialMaterial()
        {
            wireframeSolid = _mat1,
            transparent = _mat2,
        };

        try
        {
            _Dictionary.Add(key, value);

            // unset the temp values
            _key = null;
            _mat1 = null;
            _mat2 = null;
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }
}
﻿using GameplayIngredients.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameplayIngredients.Comments.Editor
{
    public class CommentsWindow : EditorWindow
    {
        [MenuItem("Window/Gameplay Ingredients/Comments")]
        public static void Open()
        {
            EditorWindow.GetWindow<CommentsWindow>();
        }

        private void OnEnable()
        {
            titleContent = EditorGUIUtility.IconContent("console.infoicon.inactive.sml");
            titleContent.text = "Comments";
            minSize = new Vector2(680, 180);
            Refresh();
            EditorSceneManager.sceneOpened += EditorSceneManager_sceneOpened;
            EditorSceneManager.sceneClosed += EditorSceneManager_sceneClosed;
            EditorSceneSetup.onSetupLoaded += EditorSceneSetup_onSetupLoaded;
            EditorSceneManager.sceneLoaded += SceneManager_sceneLoaded;
            EditorSceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
        }

        private void EditorSceneSetup_onSetupLoaded(EditorSceneSetup setup)
        {
            Refresh();
        }

        private void EditorSceneManager_sceneClosed(Scene scene)
        {
            Refresh();
        }

        private void EditorSceneManager_sceneOpened(Scene scene, OpenSceneMode mode)
        {
            Refresh();
        }

        private void SceneManager_sceneUnloaded(Scene arg0)
        {
            Refresh();
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            Refresh();
        }

        private void OnDisable()
        {
            EditorSceneManager.sceneOpened -= EditorSceneManager_sceneOpened;
            EditorSceneManager.sceneClosed -= EditorSceneManager_sceneClosed;
            EditorSceneSetup.onSetupLoaded -= EditorSceneSetup_onSetupLoaded;
            EditorSceneManager.sceneLoaded -= SceneManager_sceneLoaded;
            EditorSceneManager.sceneUnloaded -= SceneManager_sceneUnloaded;
        }

        public enum UserFilter
        {
            MyComments,
            AllComments,
        }

        const string kUserFilterPreference = "GameplayIngredients.Comments.UserFilter";
        const string kUserPreference = "GameplayIngredients.Comments.User";

        static UserFilter userFilter
        {
            get { return (UserFilter)EditorPrefs.GetInt(kUserFilterPreference, 0); }
            set { EditorPrefs.SetInt(kUserFilterPreference, (int)value); }
        }

        [InitializeOnLoadMethod]
        static void SetDefaultUser()
        {
            var user = EditorPrefs.GetString(kUserPreference, "");
            if(user == "")
            {
                user = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            }
        }

        public static string user
        {
            get { return EditorPrefs.GetString(kUserPreference, ""); }
            set { EditorPrefs.SetString(kUserPreference, value); }
        }

        bool showInfo = true;
        bool showBugs = true;
        bool showRequests = true;
        bool showTodo = true;

        bool showHigh = true;
        bool showMedium = true;
        bool showLow = true;

        int infoCount => sceneComments == null ? 0 : sceneComments.Count(o => o.comment.computedType == CommentType.Info);
        int bugCount => sceneComments == null ? 0 : sceneComments.Count(o => o.comment.computedType == CommentType.Bug);
        int requestCount => sceneComments == null ? 0 : sceneComments.Count(o => o.comment.computedType == CommentType.Request);
        int todoCount => sceneComments == null ? 0 : sceneComments.Count(o => o.comment.computedType == CommentType.ToDo);
        int highCount => sceneComments == null ? 0 : sceneComments.Count(o => o.comment.computedPriority == CommentPriority.High);
        int mediumCount => sceneComments == null ? 0 : sceneComments.Count(o => o.comment.computedPriority == CommentPriority.Medium);
        int lowCount => sceneComments == null ? 0 : sceneComments.Count(o => o.comment.computedPriority == CommentPriority.Low);

        string searchFilter;
        Vector2 scrollPosition;


        bool MatchFilter(SceneComment sc, string filter)
        {
            filter = filter.ToLowerInvariant();
            return sc.gameObject.name.Contains(filter) 
                || MatchFilter(sc.comment, filter)
                ;
        }

        bool MatchFilter(Comment comment, string filter)
        {
            return comment.title.ToLowerInvariant().Contains(filter)
                || comment.users.Any(o => o.ToLowerInvariant().Contains(filter))
                || MatchFilter(comment.message, filter)
                || comment.replies.Any(m => MatchFilter(m, filter))
                ;
        }

        bool MatchFilter(CommentMessage message, string filter)
        {
            return message.body.ToLowerInvariant().Contains(filter)
                || message.from.ToLowerInvariant().Contains(filter)
                || message.attachedObjects.Any(o => o.name.ToLowerInvariant().Contains(filter))
                || message.URL.ToLowerInvariant().Contains(filter)
                ;
        }

        private void OnGUI()
        {
            // Toolbar
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                userFilter = (UserFilter)EditorGUILayout.EnumPopup(userFilter, EditorStyles.toolbarDropDown, GUILayout.Width(140));
                if (GUILayout.Button(EditorGUIUtility.IconContent("Refresh"), EditorStyles.toolbarButton, GUILayout.Width(24)))
                    Refresh();

                searchFilter = EditorGUILayout.DelayedTextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));

                showInfo = GUILayout.Toggle(showInfo, $"Info ({infoCount})", EditorStyles.toolbarButton, GUILayout.Width(48));
                showBugs = GUILayout.Toggle(showBugs, $"Bugs ({bugCount})", EditorStyles.toolbarButton, GUILayout.Width(56));
                showRequests = GUILayout.Toggle(showRequests, $"Requests ({requestCount})", EditorStyles.toolbarButton, GUILayout.Width(80));
                showTodo = GUILayout.Toggle(showTodo, $"To Do ({todoCount})", EditorStyles.toolbarButton, GUILayout.Width(60));
                GUILayout.Space(16);
                showHigh = GUILayout.Toggle(showHigh, CommentEditor.GetPriorityContent(highCount.ToString(), CommentPriority.High), EditorStyles.toolbarButton, GUILayout.Width(32));
                showMedium = GUILayout.Toggle(showMedium, CommentEditor.GetPriorityContent(mediumCount.ToString(), CommentPriority.Medium), EditorStyles.toolbarButton, GUILayout.Width(32));
                showLow = GUILayout.Toggle(showLow, CommentEditor.GetPriorityContent(lowCount.ToString(), CommentPriority.Low), EditorStyles.toolbarButton, GUILayout.Width(32));
            }

            GUI.backgroundColor = Color.white * 1.25f;

            // Header
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Button("Comment", Styles.header, GUILayout.Width(180));
                GUILayout.Button("Description", Styles.header, GUILayout.Width(position.width - 461));
                GUILayout.Button("Location", Styles.header, GUILayout.Width(100));
                GUILayout.Button("From", Styles.header, GUILayout.Width(80));
                GUILayout.Button("Type", Styles.header, GUILayout.Width(50));
                GUILayout.Button("Status", Styles.header, GUILayout.Width(50));
            }
            GUI.backgroundColor = Color.white;
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            int i = 0;
            // Lines
            foreach (var sceneComment in sceneComments)
            {
                if (sceneComment.comment.computedType == CommentType.Bug && !showBugs) continue;
                if (sceneComment.comment.computedType == CommentType.Info && !showInfo) continue;
                if (sceneComment.comment.computedType == CommentType.Request && !showRequests) continue;
                if (sceneComment.comment.computedType == CommentType.ToDo && !showTodo) continue;
                if (sceneComment.comment.computedPriority == CommentPriority.High && !showHigh) continue;
                if (sceneComment.comment.computedPriority == CommentPriority.Medium && !showMedium) continue;
                if (sceneComment.comment.computedPriority == CommentPriority.Low && !showLow) continue;

                if (userFilter == UserFilter.MyComments && !sceneComment.comment.users.Contains(user))
                    continue;

                if (!string.IsNullOrEmpty(searchFilter) && !MatchFilter(sceneComment, searchFilter))
                    continue;

                GUI.backgroundColor = (i%2 == 0)? Color.white : Color.white * 0.9f;
                using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    
                    if(GUILayout.Button(CommentEditor.GetPriorityContent(sceneComment.comment.title, sceneComment.comment.computedPriority), Styles.line, GUILayout.Width(180)))
                    {
                        Selection.activeGameObject = sceneComment.gameObject;
                    }
                    GUILayout.Label(sceneComment.comment.message.body, Styles.line, GUILayout.Width(position.width - 461));
                    GUILayout.Label(sceneComment.gameObject.scene.name, Styles.line, GUILayout.Width(100));
                    GUILayout.Label(sceneComment.comment.message.from, Styles.line, GUILayout.Width(80));
                    GUILayout.Label(sceneComment.comment.computedType.ToString(), Styles.line, GUILayout.Width(50));
                    GUILayout.Label(sceneComment.comment.computedState.ToString(), Styles.line, GUILayout.Width(50));
                }

                i++;
            }
            EditorGUILayout.EndScrollView();
        }

        List<SceneComment> sceneComments;

        void Refresh()
        {
            sceneComments = new List<SceneComment>();
            foreach(var obj in Resources.FindObjectsOfTypeAll(typeof(SceneComment)))
            {
                sceneComments.Add((SceneComment)obj);
            }

        }

        static class Styles
        {
            public static GUIStyle header;
            public static GUIStyle sortHeader;
            public static GUIStyle line;
            static Styles()
            {
                header = new GUIStyle(EditorStyles.toolbarButton);
                header.alignment = TextAnchor.MiddleLeft;
                header.fontStyle = FontStyle.Bold;

                sortHeader = new GUIStyle(EditorStyles.toolbarDropDown);
                sortHeader.alignment = TextAnchor.MiddleLeft;
                sortHeader.fontStyle = FontStyle.Bold;

                line = new GUIStyle(EditorStyles.toolbarButton);
                line.padding = new RectOffset();
                line.contentOffset = new Vector2(6, 2);
                line.alignment = TextAnchor.UpperLeft;
                line.wordWrap = false;
            }
        }


        [SettingsProvider]
        public static SettingsProvider CommentsPreferences()
        {
            return new SettingsProvider("Preferences/Gameplay Ingredients/Comments", SettingsScope.User)
            {
                label = "Comments",
                guiHandler = (searchContext) =>
                {
                    user = EditorGUILayout.DelayedTextField("Project User Nickname", user);
                }
            };


        }

    }
}

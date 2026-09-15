@tool
extends EditorPlugin

## Registers the two Autoloads DieBody / DiceManager look up at /root/DiceSession and /root/SettingsStore.
## Enable this plugin in Project Settings → Plugins; do not rename those Autoload keys.

const SESSION_NAME := "DiceSession"
const SETTINGS_NAME := "SettingsStore"
const SESSION_PATH := "res://addons/dice_roller/Session/DiceSession.cs"
const SETTINGS_PATH := "res://addons/dice_roller/Settings/SettingsStore.cs"

func _enter_tree() -> void:
	_ensure_autoloads()


func _enable_plugin() -> void:
	_ensure_autoloads()


func _disable_plugin() -> void:
	_remove_ours(SESSION_NAME, SESSION_PATH)
	_remove_ours(SETTINGS_NAME, SETTINGS_PATH)


func _ensure_autoloads() -> void:
	_ensure_one(SESSION_NAME, SESSION_PATH)
	_ensure_one(SETTINGS_NAME, SETTINGS_PATH)


func _ensure_one(key: String, path: String) -> void:
	var setting := "autoload/%s" % key
	if ProjectSettings.has_setting(setting):
		var current := str(ProjectSettings.get_setting(setting))
		if current.contains(path):
			return
		remove_autoload_singleton(key)
	add_autoload_singleton(key, path)


func _remove_ours(key: String, path: String) -> void:
	var setting := "autoload/%s" % key
	if not ProjectSettings.has_setting(setting):
		return
	if str(ProjectSettings.get_setting(setting)).contains(path):
		remove_autoload_singleton(key)

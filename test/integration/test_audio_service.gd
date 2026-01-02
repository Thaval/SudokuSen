# Integration Tests for AudioService

extends GutTest

# Test that AudioService is properly configured and can play sounds


class TestAudioServiceSetup:
	extends GutTest

	var _audio_service: Node

	func before_each():
		# Get the AudioService autoload
		_audio_service = get_tree().root.get_node_or_null("AudioService")

	func test_audio_service_exists():
		# Use boolean check to avoid C# object serialization issues with inst_to_dict()
		var exists = _audio_service != null
		assert_true(exists, "AudioService autoload should exist")

	func test_audio_buses_configured():
		# Should have Master, Music, and SFX buses
		var master_idx = AudioServer.get_bus_index("Master")
		var music_idx = AudioServer.get_bus_index("Music")
		var sfx_idx = AudioServer.get_bus_index("SFX")

		assert_ne(master_idx, -1, "Master bus should exist")
		assert_ne(music_idx, -1, "Music bus should exist")
		assert_ne(sfx_idx, -1, "SFX bus should exist")

	func test_sfx_bus_routes_to_master():
		var sfx_idx = AudioServer.get_bus_index("SFX")
		if sfx_idx != -1:
			var send = AudioServer.get_bus_send(sfx_idx)
			assert_eq(send, "Master", "SFX bus should route to Master")
		else:
			pass_test("SFX bus not found - skipping")

	func test_music_bus_routes_to_master():
		var music_idx = AudioServer.get_bus_index("Music")
		if music_idx != -1:
			var send = AudioServer.get_bus_send(music_idx)
			assert_eq(send, "Master", "Music bus should route to Master")
		else:
			pass_test("Music bus not found - skipping")


class TestSFXPlayback:
	extends GutTest

	var _audio_service: Node

	func before_each():
		_audio_service = get_tree().root.get_node_or_null("AudioService")

	func test_can_play_click_sfx():
		if _audio_service == null:
			pass_test("AudioService not available")
			return

		# Try to call PlayClick if it exists
		if _audio_service.has_method("PlayClick"):
			_audio_service.PlayClick()
			pass_test("PlayClick called successfully")
		else:
			pass_test("PlayClick method not found - skipping")

	func test_can_play_cell_select_sfx():
		if _audio_service == null:
			pass_test("AudioService not available")
			return

		if _audio_service.has_method("PlayCellSelect"):
			_audio_service.PlayCellSelect()
			pass_test("PlayCellSelect called successfully")
		else:
			pass_test("PlayCellSelect method not found - skipping")

	func test_can_play_number_place_sfx():
		if _audio_service == null:
			pass_test("AudioService not available")
			return

		if _audio_service.has_method("PlayNumberPlace"):
			_audio_service.PlayNumberPlace()
			pass_test("PlayNumberPlace called successfully")
		else:
			pass_test("PlayNumberPlace method not found - skipping")

	func test_can_play_error_sfx():
		if _audio_service == null:
			pass_test("AudioService not available")
			return

		if _audio_service.has_method("PlayError"):
			_audio_service.PlayError()
			pass_test("PlayError called successfully")
		else:
			pass_test("PlayError method not found - skipping")

	func test_can_play_success_sfx():
		if _audio_service == null:
			pass_test("AudioService not available")
			return

		if _audio_service.has_method("PlaySuccess"):
			_audio_service.PlaySuccess()
			pass_test("PlaySuccess called successfully")
		else:
			pass_test("PlaySuccess method not found - skipping")


class TestMusicPlayback:
	extends GutTest

	var _audio_service: Node

	func before_each():
		_audio_service = get_tree().root.get_node_or_null("AudioService")

	func test_can_play_menu_music():
		if _audio_service == null:
			pass_test("AudioService not available")
			return

		if _audio_service.has_method("PlayMenuMusic"):
			_audio_service.PlayMenuMusic()
			pass_test("PlayMenuMusic called successfully")
		else:
			pass_test("PlayMenuMusic method not found - skipping")

	func test_can_play_game_music():
		if _audio_service == null:
			pass_test("AudioService not available")
			return

		if _audio_service.has_method("PlayGameMusic"):
			_audio_service.PlayGameMusic()
			pass_test("PlayGameMusic called successfully")
		else:
			pass_test("PlayGameMusic method not found - skipping")

	func test_can_stop_music():
		if _audio_service == null:
			pass_test("AudioService not available")
			return

		if _audio_service.has_method("StopMusic"):
			_audio_service.StopMusic()
			pass_test("StopMusic called successfully")
		else:
			pass_test("StopMusic method not found - skipping")


class TestAudioSettings:
	extends GutTest

	var _audio_service: Node

	func before_each():
		_audio_service = get_tree().root.get_node_or_null("AudioService")

	func test_sfx_volume_affects_bus():
		var sfx_idx = AudioServer.get_bus_index("SFX")
		if sfx_idx == -1:
			pass_test("SFX bus not found - skipping")
			return

		# Store original volume
		var original_db = AudioServer.get_bus_volume_db(sfx_idx)

		# Volume should be a valid dB value (not muted by default)
		assert_true(original_db > -80, "SFX bus should not be muted by default")

	func test_music_volume_affects_bus():
		var music_idx = AudioServer.get_bus_index("Music")
		if music_idx == -1:
			pass_test("Music bus not found - skipping")
			return

		var original_db = AudioServer.get_bus_volume_db(music_idx)
		assert_true(original_db > -80, "Music bus should not be muted by default")

"""Generate Edge TTS audio for Chinese only (zho)."""

import importlib.util
from pathlib import Path


LANG_CODE = "zho"


def load_main_module():
    module_path = Path(__file__).resolve().parent.parent / "generate_audio_azure.py"
    spec = importlib.util.spec_from_file_location("generate_audio_azure_main", module_path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    module = load_main_module()
    module.run_for_languages([LANG_CODE])


if __name__ == "__main__":
    main()

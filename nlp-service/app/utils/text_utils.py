import re
import unicodedata

_WHITESPACE = re.compile(r"\s+")


def normalize_text(value: str) -> str:
    if not isinstance(value, str):
        raise ValueError("Text must be a string.")

    normalized = unicodedata.normalize("NFKC", value)
    safe_characters = []
    for character in normalized:
        category = unicodedata.category(character)
        if category in {"Cc", "Cf", "Cs"} and character not in "\t\n\r":
            continue
        safe_characters.append(character)

    result = _WHITESPACE.sub(" ", "".join(safe_characters)).strip()
    if not result:
        raise ValueError("Text must not be empty or whitespace-only.")
    return result


def normalize_skill_key(value: str) -> str:
    normalized = unicodedata.normalize("NFKC", value)
    return _WHITESPACE.sub(" ", normalized).strip().casefold()

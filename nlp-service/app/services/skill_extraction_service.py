import json
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from app.utils.text_utils import normalize_skill_key, normalize_text


@dataclass(frozen=True)
class Skill:
    name: str
    category: str
    aliases: tuple[str, ...]
    context_aliases: tuple[str, ...]
    context_terms: tuple[str, ...]


class SkillExtractionService:
    def __init__(self, skills_file: str | Path | None = None) -> None:
        if skills_file is None:
            skills_file = Path(__file__).resolve().parents[1] / "data" / "skills.json"
        with Path(skills_file).open(encoding="utf-8") as file:
            entries = json.load(file)
        self._skills = [self._parse_entry(entry) for entry in entries]
        self._aliases: dict[str, Skill] = {}
        self._context_aliases: list[tuple[str, Skill]] = []
        for skill in self._skills:
            for alias in (skill.name, *skill.aliases):
                self._aliases[normalize_skill_key(alias)] = skill
            self._context_aliases.extend(
                (alias, skill) for alias in skill.context_aliases
            )

    @staticmethod
    def _parse_entry(entry: dict[str, Any]) -> Skill:
        return Skill(
            name=entry["name"],
            category=entry["category"],
            aliases=tuple(entry.get("aliases", [])),
            context_aliases=tuple(entry.get("context_aliases", [])),
            context_terms=tuple(
                term.casefold() for term in entry.get("context_terms", [])
            ),
        )

    @staticmethod
    def _pattern(alias: str) -> re.Pattern[str]:
        escaped = re.escape(alias).replace(r"\ ", r"\s+")
        return re.compile(
            rf"(?<![A-Za-z0-9]){escaped}(?![A-Za-z0-9])", re.IGNORECASE
        )

    def normalize_skill(self, name: str, context: str | None = None) -> str:
        key = normalize_skill_key(name)
        if context:
            normalized_context = normalize_text(context).casefold()
            for alias, skill in self._context_aliases:
                if normalize_skill_key(alias) == key and any(
                    term in normalized_context for term in skill.context_terms
                ):
                    return skill.name
        skill = self._aliases.get(key)
        return skill.name if skill else " ".join(name.split())

    def extract(self, text: str) -> list[dict[str, str]]:
        document = normalize_text(text)
        candidates: list[tuple[int, int, int, Skill]] = []
        for skill in self._skills:
            for alias in (skill.name, *skill.aliases):
                for match in self._pattern(alias).finditer(document):
                    candidates.append((match.start(), match.end(), 0, skill))

            for alias in skill.context_aliases:
                for match in self._pattern(alias).finditer(document):
                    context = document[max(0, match.start() - 80) : match.end() + 80]
                    if any(term in context.casefold() for term in skill.context_terms):
                        candidates.append((match.start(), match.end(), 1, skill))

        # Prefer contextual equivalences and the longest match when aliases overlap.
        candidates.sort(
            key=lambda item: (item[0], -item[2], -(item[1] - item[0]), item[3].name)
        )
        selected: list[tuple[int, int, Skill]] = []
        for start, end, _priority, skill in candidates:
            if any(start < current_end and end > current_start
                   for current_start, current_end, _current_skill in selected):
                continue
            selected.append((start, end, skill))

        results: list[tuple[int, dict[str, str]]] = []
        seen: set[str] = set()
        for start, _end, skill in selected:
            if skill.name not in seen:
                seen.add(skill.name)
                results.append(
                    (start, {"name": skill.name, "category": skill.category})
                )
        return [item for _position, item in sorted(results, key=lambda result: result[0])]

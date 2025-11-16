from langchain_docling import DoclingLoader
from pathlib import Path
import subprocess
import sys
from inspect import signature

FILE_PATH = Path("pdfs/The Great Gatsby.pdf")
OUTPUT_PATH = Path("output/The Great Gatsby.md")

def make_loader(file_path: Path, engine: str | None, lang: str = "eng") -> DoclingLoader:
    kwargs = {}
    try:
        params = set(signature(DoclingLoader).parameters.keys())
        if engine is None:
            return DoclingLoader(file_path=str(file_path))
        # enable OCR
        for k in ("ocr", "use_ocr", "enable_ocr"):
            if k in params:
                kwargs[k] = True
                break
        for k in ("ocr_engine", "ocr_provider", "engine"):
            if k in params:
                kwargs[k] = engine
                break
        for k in ("ocr_lang", "ocr_langs", "lang", "language"):
            if k in params:
                kwargs[k] = lang
                break
        for k in ("dpi", "ocr_dpi"):
            if k in params:
                kwargs[k] = 300
                break
    except Exception:
        pass
    return DoclingLoader(file_path=str(file_path), **kwargs)

def ocr_with_ocrmypdf(src: Path, lang: str = "eng") -> Path:
    dst = src.with_name(src.stem + "_ocr.pdf")
    cmd = [sys.executable, "-m", "ocrmypdf", "--force-ocr", "--language", lang, str(src), str(dst)]
    subprocess.run(cmd, check=True)
    return dst

def load_with_fallbacks(src: Path):
    for engine in ("rapidocr", "tesseract", "paddleocr", None):
        try:
            docs = make_loader(src, engine).load()
        except Exception:
            continue
        text = "".join((d.page_content or "") for d in docs).strip()
        if text:
            print(f"OCR engine used: {engine or 'none'}")
            return docs
        else:
            print(f"{engine or 'no-ocr'} returned empty; trying next...")
    print("All engines empty; running ocrmypdf --force-ocr ...")
    ocred = ocr_with_ocrmypdf(src)
    return make_loader(ocred, None).load()

def main():
    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    docs = load_with_fallbacks(FILE_PATH)
    markdown_text = "\n\n".join(d.page_content.strip() for d in docs if d.page_content).strip()
    OUTPUT_PATH.write_text(markdown_text, encoding="utf-8")
    print(f"Wrote Markdown to {OUTPUT_PATH}")

if __name__ == "__main__":
    main()
import os
import re
import sys
import argparse
from pathlib import Path

def replace_links(file_path, repo_url, branch='main'):
    """处理 Markdown 文件中的相对链接，生成 DocFX 兼容的外部链接"""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()

    original_content = content

    link_pattern = r'\[([^\]]+)\]\(([^)]+)\)'

    def replace_link(match):
        display_text = match.group(1)
        link_url = match.group(2)

        if link_url.startswith(('http://', 'https://', 'mailto:', '#', 'xref:')):
            return match.group(0)

        is_directory = (link_url.endswith('/') or
                        '.' not in os.path.basename(link_url) or
                        os.path.basename(link_url).split('.')[-1] in ['', '/'])
        url_type = 'tree' if is_directory else 'blob'

        if link_url.startswith('./'):
            relative_path = link_url[2:]
            new_url = f'{repo_url}/{url_type}/{branch}/{relative_path}'
        elif link_url.startswith('../'):
            file_dir = os.path.dirname(file_path)
            abs_path = os.path.normpath(os.path.join(file_dir, link_url))
            repo_relative_path = os.path.relpath(abs_path, start='.')
            new_url = f'{repo_url}/{url_type}/{branch}/{repo_relative_path}'
        elif link_url.startswith('/'):
            new_url = f'{repo_url}/{url_type}/{branch}/{link_url[1:]}'
        else:
            file_dir = os.path.dirname(file_path)
            abs_path = os.path.normpath(os.path.join(file_dir, link_url))
            repo_relative_path = os.path.relpath(abs_path, start='.')
            new_url = f'{repo_url}/{url_type}/{branch}/{repo_relative_path}'

        return f'[{display_text}]({new_url})'

    content = re.sub(link_pattern, replace_link, content)

    if content != original_content:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Processed links in {file_path}")
        return True
    else:
        print(f"No changes needed for {file_path}")
        return False

def main():
    parser = argparse.ArgumentParser(description='Process relative links in Markdown files for DocFX')
    parser.add_argument('--repo-url', required=True, help='GitHub repository URL')
    parser.add_argument('--branch', default='main', help='Repository branch name')
    parser.add_argument('--docs-dir', default='Documentation', help='Documentation directory path')
    parser.add_argument('--verbose', action='store_true', help='Enable verbose output')

    args = parser.parse_args()

    if args.verbose:
        print(f"Repository: {args.repo_url}")
        print(f"Branch: {args.branch}")
        print(f"Documentation directory: {args.docs_dir}")

    processed_count = 0

    docs_path = Path(args.docs_dir)
    if not docs_path.exists():
        print(f"Documentation directory not found: {args.docs_dir}")
        sys.exit(1)

    for md_file in docs_path.rglob('*.md'):
        if args.verbose:
            print(f"Processing: {md_file}")

        try:
            if replace_links(str(md_file), args.repo_url, args.branch):
                processed_count += 1
        except Exception as e:
            print(f"Error processing {md_file}: {str(e)}")

    print(f"Successfully processed {processed_count} Markdown files")

if __name__ == '__main__':
    main()
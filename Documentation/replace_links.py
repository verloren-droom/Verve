import os
import re
import sys
import argparse
from pathlib import Path

def replace_links(file_path, repo_url, branch='main'):
    """处理 Markdown 文件中的相对链接，精确替换不同类型的链接"""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    original_content = content
    
    link_pattern = r'\[([^\]]+)\]\(([^)]+)\)'
    
    def replace_link(match):
        display_text = match.group(1)
        link_url = match.group(2)
        
        if link_url.startswith(('http://', 'https://', 'mailto:', '#')):
            return match.group(0)

        if (link_url.endswith('/') or 
            '.' not in os.path.basename(link_url) or
            os.path.basename(link_url).split('.')[-1] in ['', '/']):
            url_type = 'tree'
        else:
            url_type = 'blob'
        
        if link_url.startswith('./'):
            # ./ 开头的相对路径 - 相对于根目录
            relative_path = link_url[2:]
            new_url = f'{repo_url}/{url_type}/{branch}/{relative_path}'
        elif link_url.startswith('../'):
            # ../ 开头的相对路径 - 上一级目录
            file_dir = os.path.dirname(file_path)
            abs_path = os.path.normpath(os.path.join(file_dir, link_url))
            repo_relative_path = os.path.relpath(abs_path, start='.')
            new_url = f'{repo_url}/{url_type}/{branch}/{repo_relative_path}'
        elif link_url.startswith('/'):
            # / 开头的绝对路径 - 相对于仓库根目录
            new_url = f'{repo_url}/{url_type}/{branch}/{link_url[1:]}'
        else:
            # 直接文件名 - 相对于当前文件所在目录
            file_dir = os.path.dirname(file_path)
            abs_path = os.path.normpath(os.path.join(file_dir, link_url))
            repo_relative_path = os.path.relpath(abs_path, start='.')
            new_url = f'{repo_url}/{url_type}/{branch}/{repo_relative_path}'
        
        return f'[{display_text}]({new_url})'
    
    content = re.sub(link_pattern, replace_link, content)
    
    image_pattern = r'!\[([^\]]*)\]\(([^)]+)\)'
    
    def replace_image(match):
        alt_text = match.group(1)
        image_src = match.group(2)
        
        if image_src.startswith(('http://', 'https://')):
            return match.group(0)
        
        url_type = 'blob'
        
        if image_src.startswith('./'):
            relative_path = image_src[2:]
            new_src = f'{repo_url}/{url_type}/{branch}/{relative_path}'
        elif image_src.startswith('../'):
            file_dir = os.path.dirname(file_path)
            abs_path = os.path.normpath(os.path.join(file_dir, image_src))
            repo_relative_path = os.path.relpath(abs_path, start='.')
            new_src = f'{repo_url}/{url_type}/{branch}/{repo_relative_path}'
        elif image_src.startswith('/'):
            new_src = f'{repo_url}/{url_type}/{branch}/{image_src[1:]}'
        else:
            file_dir = os.path.dirname(file_path)
            abs_path = os.path.normpath(os.path.join(file_dir, image_src))
            repo_relative_path = os.path.relpath(abs_path, start='.')
            new_src = f'{repo_url}/{url_type}/{branch}/{repo_relative_path}'
        
        return f'![{alt_text}]({new_src})'
    
    content = re.sub(image_pattern, replace_image, content)
    
    if content != original_content:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Processed links in {file_path}")
        return True
    else:
        print(f"No changes needed for {file_path}")
        return False

def main():
    parser = argparse.ArgumentParser(description='Process relative links in Markdown files')
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
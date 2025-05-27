import type { Action } from 'svelte/action';
import twemoji from '@twemoji/api';

export interface TwemojiOptions {
  folder?: string;
  ext?: string;
  className?: string;
  base?: string;
  attributes?: () => Record<string, string>;
}

export const emojify: Action<HTMLElement, TwemojiOptions> = (node, options = {}) => {
  const defaultOptions: TwemojiOptions = {
    folder: 'svg',
    ext: '.svg',
    className: 'twemoji',
  };

  let config = { ...defaultOptions, ...options };

  const parse = () => {
    node.innerHTML = twemoji.parse(node.innerHTML, config);
  };

  parse();

  return {
    update(newOptions: TwemojiOptions) {
      config = { ...defaultOptions, ...newOptions };
      parse();
    },
    destroy() { },
  };
};
